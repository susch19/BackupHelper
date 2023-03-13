using BackupService;
using BackupService.Scheduling;

using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;
Logger logger = null;
try
{
    var builder = Host.CreateApplicationBuilder(args);

    var config = builder
        .Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    logger = LogManager.Setup()
                       .LoadConfigurationFromSection(config)
                       .GetCurrentClassLogger();
    
    builder.Services
        .AddSingleton<BackupDiffer>()
        .AddHostedService<ScheduleManager>()
        .Configure<BackupsConfig>(builder.Configuration.GetSection(BackupsConfig.ConfigName))
        .AddLogging(l =>
        {
            l.ClearProviders();
            l.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            l.AddNLog(config);
        });



    var host = builder.Build();

    host.Run();

}
catch (Exception ex)
{
    logger?.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}