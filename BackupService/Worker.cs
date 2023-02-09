namespace BackupService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;


    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var bd = new BackupDiffer();
        var source = @"G:\Backups\TestBackup\Source";
        var target = "G:\\Backups\\TestBackup\\Result";
        var backupType = BackupType.Full;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Worker running at: {DateTimeOffset.Now} with {backupType}");
            try
            {
                var changes = bd.GetChangedFiles(source, true, backupType);

                if (changes is not null)
                {

                    bd.BackupDetectedChanges(changes, source, target, "123", backupType);
                    bd.StoreNewChangesInIndex(changes);
                }
                backupType = backupType == BackupType.Incremental ? BackupType.Differential : BackupType.Incremental;
            }
            finally
            {

            }
            await Task.Delay(10000, stoppingToken);
        }
    }
}
