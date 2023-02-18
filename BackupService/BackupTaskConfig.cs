using BackupService.Events;
using BackupService.Scheduling;

namespace BackupService;

[Nooson]
public partial class BackupTaskConfig
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }

    public bool Enabled { get; set; } = true;
    public string Path { get; set; }
    public bool Recursive { get; set; } = true;
    public string Password { get; set; }
    public List<string> BackupSources { get; set; }
    public List<string> TargetSources { get; set; }

    public List<BackupEvent> ProgrammEvents { get; set; }

    public List<Schedule> Schedules { get; set; }

}
