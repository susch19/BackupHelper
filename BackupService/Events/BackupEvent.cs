namespace BackupService.Events;

[NoosonDynamicType(typeof(ProgrammEvent), typeof(ServiceEvent))]
public abstract class BackupEvent
{
    public bool Enabled { get; set; }
    public ActionRelativeToBackup BackupEventExecutionTime { get; set; }
}
