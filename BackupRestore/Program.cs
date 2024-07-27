using BackupRestore.Data;

using Photino.Blazor;

using Radzen;

using SevenZip;

namespace BackupRestore;

public class Programm
{
    [STAThread]
    public static void Main(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();


        SevenZipBase.SetLibraryPath(configuration.GetValue<string>("SevenZipDllPath"));
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // Add services to the container.
        builder.Services.AddSingleton<RecoveryService>();
        builder.RootComponents.Add<App>("app");
        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<ConfigService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();

        var app = builder.Build();

        app.MainWindow
#if DEBUG
            .SetDevToolsEnabled(true)
#endif
            .SetWidth(1400)
            .SetHeight(700)
            .SetLogVerbosity(0)
            .SetTitle("BackupRestore");

        //AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        //{
        //    app.MainWindow.OpenAlertWindow("Fatal exception", error.ExceptionObject.ToString());
        //};
        app.Run();
    }

}