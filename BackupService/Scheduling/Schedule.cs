using Backup.Shared;

namespace BackupService.Scheduling;

[NoosonDynamicType(typeof(MutliPeriodSchedule), typeof(IntervalSchedule), typeof(CronSchedule), typeof(PeriodSchedule))]
public abstract class Schedule : ISchedule, ICloneableGeneric<Schedule>
{
    public DateTime LastRun { get; set; }
    public BackupType BackupType { get; set; }

    public abstract DateTime? NextOccurence(DateTime dt);

    protected virtual Schedule Clone() => throw new NotImplementedException();

    Schedule ICloneableGeneric<Schedule>.Clone()
    {
        return Clone();
    }
}
