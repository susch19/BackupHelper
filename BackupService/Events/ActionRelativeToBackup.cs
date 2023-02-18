namespace BackupService.Events;

[Flags]
public enum ActionRelativeToBackup
{
    None,
    Before = 1 << 1,
    After = 1 << 2
}
