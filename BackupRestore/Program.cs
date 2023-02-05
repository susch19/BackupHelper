using BackupRestore.Data;

using MatBlazor;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Photino.Blazor;

using SevenZip;
namespace BackupRestore;

public class Programm
{
    [STAThread]
    public static void Main(string[] args)
    {
        SevenZipBase.SetLibraryPath("C:\\Program Files\\7-Zip\\7z.dll");
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // Add services to the container.
        builder.Services.AddSingleton<RecoveryService>();
        builder.RootComponents.Add<App>("app");
        builder.Services.AddMatBlazor();

        var app = builder.Build();

        app.MainWindow
#if DEBUG
            .SetDevToolsEnabled(true)
#endif
            .SetWidth(1400)
            .SetHeight(700)
            .SetLogVerbosity(0)
            .SetTitle("BackupRestore");

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.OpenAlertWindow("Fatal exception", error.ExceptionObject.ToString());
        };
        app.Run();
    }

}