using Backup.Shared;

using Microsoft.Extensions.Options;

using SevenZip;

using System.Text;
namespace BackupService.Scheduling;

public class ScheduleManager : BackgroundService
{
    public record struct NextSchedule(ISchedule Schedule, DateTime RunAt, BackupTaskConfig Config);

    private readonly List<BackupTaskConfig> backupConfigs;
    private readonly BackupDiffer backupDiffer;
    private readonly ILogger<ScheduleManager> logger;
    private readonly List<NextSchedule> nextSchedules = new();
    private readonly IOptionsMonitor<BackupsConfig> config;
    private bool saveCredential = true;

    public ScheduleManager(BackupDiffer backupDiffer, IOptionsMonitor<BackupsConfig> config, ILogger<ScheduleManager> logger)
    {
        this.config = config;
        SevenZipBase.SetLibraryPath(this.config.CurrentValue.SevenZipPath);
        this.backupDiffer = backupDiffer;
        this.logger = logger;
        backupConfigs = new();
    }

    public List<NextSchedule> WhatsNext(DateTime from, DateTime to)
    {
        nextSchedules.Clear();
        CheckBackupConfigs(config.CurrentValue, backupConfigs, logger, ref saveCredential);
        if (backupConfigs.Count == 0)
            return nextSchedules;
        TimeSpan offset = GetOffset(ref from);
        to = to.ToUniversalTime().Add(offset);

        foreach (var backupConfig in backupConfigs)
        {
            if (!backupConfig.Enabled)
                continue;
            var schedules = backupConfig.Schedules;
            if (schedules.Count == 0)
                continue;
            DateTime current = DateTime.MaxValue;
            ISchedule? currentSched = null;

            foreach (var item in schedules)
            {
                var nexter = item.NextOccurence(from);
                if (nexter is not null && current >= nexter.Value && nexter.Value <= to)
                {
                    current = nexter.Value;
                    currentSched = item;
                }
            }

            if (currentSched is not null && current != DateTime.MaxValue)
            {
                nextSchedules.Add(new(currentSched, current, backupConfig));
            }
        }

        return nextSchedules;
    }

    private static TimeSpan GetOffset(ref DateTime from)
    {
        from = from.ToUniversalTime();
        var offset = from.ToLocalTime() - from;
        from = from.Add(offset);
        return offset;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //var latestRun = backupConfig.Schedules.Max(x=>x.LastRun); //For executing missed runs
        DateTime lastRun = DateTime.UtcNow;// new DateTime(2023, 02, 22, 16, 19, 59, DateTimeKind.Utc);

        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
                stoppingToken.ThrowIfCancellationRequested();

            var secondsToAdd = DateTime.UtcNow - lastRun;
            var future = lastRun.Add(secondsToAdd);
            var next = WhatsNext(lastRun, future);
            try
            {
                if (next.Count == 0)
                {
                    await Task.Delay(15000, stoppingToken);
                    continue;
                }
                var lastRunLocalTime = lastRun;
                var offset = GetOffset(ref lastRunLocalTime);

                foreach (var nextSchedule in next.OrderBy(x => x.RunAt))
                {
                    await ProcessSchedule(lastRunLocalTime, offset, nextSchedule, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went wrong during the backup");
            }
            finally
            {
                lastRun = future;
            }
        }
    }

    private async Task ProcessSchedule(DateTime lastRunLocalTime, TimeSpan offset, NextSchedule nextSchedule, CancellationToken stoppingToken)
    {
        var scheduleConfig = nextSchedule.Config;
        var runAt = nextSchedule.RunAt.Add(offset * -1) - lastRunLocalTime;
        if (runAt > TimeSpan.FromMilliseconds(100))
            await Task.Delay(runAt, stoppingToken);
        nextSchedule.Schedule.LastRun = DateTime.UtcNow;
        foreach (var item in scheduleConfig.ProgrammEvents
            .Where(x => x.Enabled
                && (x.BackupEventExecutionTime & Events.ActionRelativeToBackup.Before) > 0))
        {
            item.Execute();
        }

        var sevenZipPW = scheduleConfig.Password;
        if (scheduleConfig.CredentialManager)
            sevenZipPW = Encoding.UTF8.GetString(CredentialHelper.GetCredentialsFor(sevenZipPW, "", "Passwort for 7zip Backup", ref saveCredential));

        var backupType = nextSchedule.Schedule.BackupType;

        BackupType detectedBackupType = BackupType.Full;
        Dictionary<string, BackupFileChange> changes = new();
        for (int sourceI = 0; sourceI < scheduleConfig.BackupSources.Count; sourceI++)
        {
            string? source = scheduleConfig.BackupSources[sourceI];
            logger.LogInformation("Starting backup check for {}", source);

            var newChanges = backupDiffer.GetChangedFiles(source, scheduleConfig.BackupIgnorePath, scheduleConfig.BackupIndexPath, scheduleConfig.FastDifferenceCheck, scheduleConfig.Recursive, backupType);
            if (newChanges is null)
                continue;

            if (!scheduleConfig.SplitArchives)
            {
                if (changes is null)
                {
                    changes = newChanges.Value.changes;
                    detectedBackupType = newChanges.Value.backupType;
                }
                else
                {
                    foreach (var item in newChanges.Value.changes)
                    {
                        changes[item.Key] = item.Value;
                    }
                    if (detectedBackupType < newChanges.Value.backupType)
                        detectedBackupType = newChanges.Value.backupType;
                }

                continue;
            }
            else
            {
                changes = newChanges.Value.changes;
                detectedBackupType = newChanges.Value.backupType;
                BackupFiles(scheduleConfig, sevenZipPW, detectedBackupType, changes, source);
            }

            logger.LogInformation("Finished backup {}", scheduleConfig.Path);
        }

        if (!scheduleConfig.SplitArchives && changes.Count > 0)
        {
            BackupFiles(scheduleConfig, sevenZipPW, detectedBackupType, changes, "");
        }



        foreach (var item in scheduleConfig.ProgrammEvents
            .Where(x => x.Enabled
                && (x.BackupEventExecutionTime & Events.ActionRelativeToBackup.After) > 0))
        {
            item.Execute();
        }
        logger.LogInformation("Completed backup for {}", scheduleConfig.Path);
    }

    private void BackupFiles(BackupTaskConfig scheduleConfig, string sevenZipPW, BackupType detectedBackupType, Dictionary<string, BackupFileChange> changes, string source)
    {
        if (changes.Count > 0)
        {
            logger.LogInformation("Backuping {} Files for backup {}", changes.Count, scheduleConfig.Path);
            var firstTarget = "";
            for (int i = 0; i < scheduleConfig.TargetSources.Count; i++)
            {
                var target = scheduleConfig.TargetSources[i];
                Directory.CreateDirectory(target);
                if (!string.IsNullOrWhiteSpace(firstTarget))
                {
                    if (string.IsNullOrWhiteSpace(source))
                        File.Copy(firstTarget, BackupDiffer.GetBackupFileName(scheduleConfig.ArchiveName, target, detectedBackupType));
                    else
                        File.Copy(firstTarget, BackupDiffer.GetBackupFileName(new DirectoryInfo(source).Name, target, detectedBackupType));
                    continue;
                }

                firstTarget = backupDiffer.BackupDetectedChanges(changes, source, target, sevenZipPW, scheduleConfig.ArchiveName, detectedBackupType);
            }

            backupDiffer.StoreNewChangesInIndex(changes, scheduleConfig.BackupIndexPath);
        }
    }

    public static void CheckBackupConfigs(BackupsConfig config, List<BackupTaskConfig> backupConfigs, ILogger? logger, ref bool saveCredential)
    {
        var enabledConfigs = backupConfigs.Where(x => x.Enabled).ToArray();

        if (enabledConfigs.Length == config.ConfigPaths.Count
            && !enabledConfigs.Any(x => config.ConfigPaths.All(y => !x.Path.Equals(y))))
            return;

        backupConfigs.Clear();

        foreach (var path in config.ConfigPaths)
        {
            try
            {
                var credentialName = string.IsNullOrWhiteSpace(path.CredentialName) ? config.DefaultCredentialName : path.CredentialName;
                var password = string.IsNullOrWhiteSpace(path.Password) ? config.DefaultPassword : path.Password;

                var pw = CredentialHelper.GetCredentialsFor(credentialName, password, "Config Password", ref saveCredential);

                if (File.Exists(path.Path))
                {
                    using var br = BackupEncryptionHelper.OpenEncryptedReaderFor(File.OpenRead(path.Path), pw);
                    var taskConfig = BackupTaskConfig.Deserialize(br);
                    backupConfigs.Add(taskConfig);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during config parsing");
            }
        }
    }
}
