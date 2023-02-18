using Cronos;

namespace BackupService.Scheduling;

[Nooson]
public partial class CronSchedule : Schedule
{
    public string Expression { get; set; }

    public override DateTime? NextOccurence(DateTime dt)
    {
        return CronExpression.Parse(Expression).GetNextOccurrence(dt);
    }
}
