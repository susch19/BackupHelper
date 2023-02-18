using BackupService;
using BackupService.Scheduling;

using SevenZip;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services
    .AddSingleton<BackupsConfig>()
    .AddSingleton<BackupDiffer>()
    .AddHostedService<ScheduleManager>()
    .Configure<BackupsConfig>(builder.Configuration.GetSection(BackupsConfig.ConfigName));

var host = builder.Build();

host.Run();

