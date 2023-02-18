namespace BackupService.Scheduling;

[Nooson]
public partial class IntervalSchedule : Schedule
{
    public uint SecondsAfter { get; set; }

    public override DateTime? NextOccurence(DateTime dt)
    {
        return dt.AddSeconds(SecondsAfter);
    }
}
