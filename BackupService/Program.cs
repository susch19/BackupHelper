using BackupService;

using System.Security.Cryptography;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

var bd = new BackupDiffer();
bd.Backup("C:\\Users\\susch\\source\\repos\\AppBroker", ".");

host.Run();
