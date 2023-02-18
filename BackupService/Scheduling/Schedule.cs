namespace BackupService.Scheduling;

[NoosonDynamicType(typeof(PeriodSchedule), typeof(IntervalSchedule), typeof(CronSchedule))]
public abstract class Schedule : ISchedule
{
    public DateTime LastRun { get; set; }
    public BackupType BackupType { get; set; }

    public abstract DateTime? NextOccurence(DateTime dt);
}
