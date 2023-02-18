namespace BackupService.Events;

[Nooson]
public partial class ProgrammEvent : BackupEvent
{
    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
    public string Path { get; set; }
    public string Arguments { get; set; }
}
