namespace BackupService.Scheduling;

[Nooson]
public partial class IntervalSchedule : Schedule
{
    public uint SecondsAfter { get; set; }

    public override DateTime? NextOccurence(DateTime dt)
    {
        var nextRun = LastRun.AddSeconds(SecondsAfter);
        if (nextRun < dt)
            return dt;
        return nextRun;
    }
    protected override Schedule Clone()
    {
        return new IntervalSchedule() { BackupType = BackupType, LastRun = LastRun, SecondsAfter = SecondsAfter };
    }
}
