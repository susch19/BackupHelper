using BackupService.Events;
using BackupService.Scheduling;

namespace BackupService;

[Nooson]
public partial class BackupTaskConfig
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "";

    [NoosonVersioning(nameof(IsVersion1), "", nameof(Version))]
    public string BackupIgnorePath { get; set; } = "";
    public bool Recursive { get; set; } = true;
    public string Password { get; set; } = "";

    [NoosonVersioning(nameof(IsVersion2), "", nameof(Version))]
    public bool CredentialManager { get; set; } = true;

    public List<string> BackupSources { get; set; } = new();
    public List<string> TargetSources { get; set; } = new();

    public List<BackupEvent> ProgrammEvents { get; set; } = new();

    public List<Schedule> Schedules { get; set; } = new();

    private static bool IsVersion1(int version)
    {
        return version > 0;
    }
    private static bool IsVersion2(int version)
    {
        return version > 1;
    }

}
