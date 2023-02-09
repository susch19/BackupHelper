using BackupService;

using System.Security.Cryptography;
using SevenZip;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

SevenZipBase.SetLibraryPath("C:\\Program Files\\7-Zip\\7z.dll");
var bd = new BackupDiffer();
//var source = @"G:\Backups\TestBackup\Source";
//var target = "G:\\Backups\\TestBackup\\Result";
//var backupType = BackupType.Incremental;
//var changes = bd.GetChangedFiles(source, true, backupType);

//if (changes is null)
//    return;
//bd.BackupDetectedChanges(changes, source, target, "123", backupType);
//bd.StoreNewChangesInIndex(changes);
host.Run();
