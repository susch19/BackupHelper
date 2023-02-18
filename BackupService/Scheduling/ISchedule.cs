namespace BackupService.Scheduling;

public interface ISchedule
{
    DateTime LastRun { get; set; }
    BackupType BackupType { get; set; }
    public DateTime? NextOccurence(DateTime dt);
}
