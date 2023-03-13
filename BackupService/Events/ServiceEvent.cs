namespace BackupService.Events;

[Nooson]
public partial class ServiceEvent : BackupEvent
{

    [NoosonOrder(int.MinValue)]
    public int Version { get; set; }
}
