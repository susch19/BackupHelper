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
    private readonly BackupsConfig config;
    private bool saveCredential = true;

    public ScheduleManager(BackupDiffer backupDiffer, IOptions<BackupsConfig> config, ILogger<ScheduleManager> logger)
    {
        this.config = config.Value;
        SevenZipBase.SetLibraryPath(this.config.SevenZipPath);
        this.backupDiffer = backupDiffer;
        this.logger = logger;
        backupConfigs = new();
    }

    public List<NextSchedule> WhatsNext(DateTime from, DateTime to)
    {
        nextSchedules.Clear();
        CheckBackupConfigs(config, backupConfigs, ref saveCredential);
        if (backupConfigs.Count == 0)
            return nextSchedules;
        from = from.ToUniversalTime();
        var offset = from.ToLocalTime() - from;
        from = from.Add(offset);
        to = to.ToUniversalTime().Add(offset);

        foreach (var backupConfig in backupConfigs)
        {
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

                foreach (var nextSchedule in next.OrderBy(x => x.RunAt))
                {
                    var runAt = nextSchedule.RunAt - lastRun;
                    if (runAt > TimeSpan.FromMilliseconds(100))
                        await Task.Delay(runAt, stoppingToken);
                    nextSchedule.Schedule.LastRun = DateTime.UtcNow;
                    foreach (var item in nextSchedule.Config.ProgrammEvents
                        .Where(x => x.Enabled
                            && (x.BackupEventExecutionTime & Events.ActionRelativeToBackup.Before) > 0))
                    {
                        //item Execute
                    }

                    var sevenZipPW = nextSchedule.Config.Password;
                    if (nextSchedule.Config.CredentialManager)
                        sevenZipPW = Encoding.UTF8.GetString(CredentialHelper.GetCredentialsFor(sevenZipPW, "", "Passwort for 7zip Backup", ref saveCredential));

                    var backupType = nextSchedule.Schedule.BackupType;
                    foreach (var source in nextSchedule.Config.BackupSources)
                    {
                        var changesWithBackupType = backupDiffer.GetChangedFiles(source, nextSchedule.Config.BackupIgnorePath, true, nextSchedule.Config.Recursive
                            , backupType);
                        if (changesWithBackupType is not null)
                        {
                            backupType = changesWithBackupType.Value.backupType;
                            var changes = changesWithBackupType.Value.changes;
                            var firstTarget = "";
                            for (int i = 0; i < nextSchedule.Config.TargetSources.Count; i++)
                            {
                                var target = nextSchedule.Config.TargetSources[i];
                                Directory.CreateDirectory(target);
                                if (!string.IsNullOrWhiteSpace(firstTarget))
                                {
                                    File.Copy(firstTarget, BackupDiffer.GetBackupFileName(new DirectoryInfo(source), target, backupType));
                                    continue;
                                }

                                firstTarget = backupDiffer.BackupDetectedChanges(changes, source, target, sevenZipPW, backupType);
                            }

                            backupDiffer.StoreNewChangesInIndex(changes);
                        }
                    }


                    foreach (var item in nextSchedule.Config.ProgrammEvents
                        .Where(x => x.Enabled
                            && (x.BackupEventExecutionTime & Events.ActionRelativeToBackup.After) > 0))
                    {
                        //item Execute
                    }
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

    public static void CheckBackupConfigs(BackupsConfig config, List<BackupTaskConfig> backupConfigs, ref bool saveCredential)
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
            catch (Exception)
            {
            }
        }
    }
}
