using Cronos;

namespace BackupService.Scheduling;

[Nooson]
public partial class CronSchedule : Schedule
{
    public string Expression { get; set; }

    [NoosonIgnore]
    public CronExpression CronExpression => CronExpression.Parse(Expression);

    public override DateTime? NextOccurence(DateTime dt)
    {
        try
        {
            return CronExpression.GetNextOccurrence(dt);

        }
        catch (Exception)
        {
            return null;
        }
    }

    protected override Schedule Clone()
    {
        return new CronSchedule() { BackupType = BackupType, LastRun = LastRun, Expression = Expression };
    }
}
