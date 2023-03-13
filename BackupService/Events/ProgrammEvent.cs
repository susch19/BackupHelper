using System.Diagnostics;

namespace BackupService.Events;

[Nooson]
public partial class ProgrammEvent : BackupEvent
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
    public string Path { get; set; }
    public string Arguments { get; set; }

    [NoosonVersioning(nameof(IsVersion1), "false", nameof(Version))]
    public bool WaitForExit { get; set; }

    public override void Execute()
    {
        var process = Process.Start(Path, Arguments);
        if (WaitForExit)
            process.WaitForExit();
    }

    private static bool IsVersion1(int version)
    {
        return version > 0;
    }
}
