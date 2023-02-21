using Backup.Shared;

namespace BackupService.Scheduling;

[Nooson]
public partial class MutliPeriodSchedule : Schedule
{
    public DaysOfWeek DaysOfWeek { get; set; }

    public List<TimeOnly> TimesOfDay { get; set; }


    public override DateTime? NextOccurence(DateTime dt)
    {
        if (DaysOfWeek == DaysOfWeek.None || TimesOfDay.Count == 0)
            return null;

        var dayOfWeek = 1 << (int)dt.DayOfWeek;
        var daysOfWeek = (int)DaysOfWeek;
        if ((dayOfWeek & daysOfWeek) > 0)
        {
            var to = new TimeOnly(dt.TimeOfDay.Ticks);
            if (TimesOfDay.Any(x => x >= to))
                return DateTime.Now.Date.AddTicks(TimesOfDay.Order().First(x => x >= to).Ticks);
        }

        DateTime? DateTimeBasedOnNextFlag(int bigger)
        {

            int shifted = 0;
            while (bigger > 0)
            {
                if ((bigger & 1) == 1)
                    return dt.AddDays(shifted).Date.AddTicks(TimesOfDay.Order().First().Ticks);
            }
            return null;
        }

        if (daysOfWeek > ((dayOfWeek - 1) << 1))
        {
            var bigger = dayOfWeek ^ (dayOfWeek - 1);
            return DateTimeBasedOnNextFlag(bigger);
        }

        return DateTimeBasedOnNextFlag(daysOfWeek);
    }
    protected override Schedule Clone()
    {
        return new MutliPeriodSchedule() { BackupType = BackupType, TimesOfDay = TimesOfDay.ToList(), DaysOfWeek = DaysOfWeek, LastRun = LastRun };
    }
}


[Nooson]
public partial class PeriodSchedule : Schedule
{
    public DaysOfWeek DaysOfWeek { get; set; }

    public TimeOnly TimeOfDay { get; set; }

    [NoosonIgnore]
    public List<DaysOfWeek> SelectedDaysOfWeek { get; set; } = new();
    [NoosonIgnore]
    public DateTime TimeOfDayDate 
    { 
        get => DateTime.Now.Date.AddTicks(TimeOfDay.Ticks); 
        set => TimeOfDay = new(value.TimeOfDay.Ticks); 
    }


    public override DateTime? NextOccurence(DateTime dt)
    {
        if (DaysOfWeek == DaysOfWeek.None)
            return null;

        var dayOfWeek = 1 << (int)dt.DayOfWeek;
        var daysOfWeek = (int)DaysOfWeek;
        if ((dayOfWeek & daysOfWeek) > 0)
        {
            var to = new TimeOnly(dt.TimeOfDay.Ticks);

            return DateTime.Now.Date.AddTicks(TimeOfDay.Ticks);
        }

        DateTime? DateTimeBasedOnNextFlag(int bigger)
        {

            int shifted = 0;
            while (bigger > 0)
            {
                if ((bigger & 1) == 1)
                    return dt.AddDays(shifted).Date.AddTicks(TimeOfDay.Ticks);
            }
            return null;
        }

        if (daysOfWeek > ((dayOfWeek - 1) << 1))
        {
            var bigger = dayOfWeek ^ (dayOfWeek - 1);
            return DateTimeBasedOnNextFlag(bigger);
        }

        return DateTimeBasedOnNextFlag(daysOfWeek);
    }
    protected override Schedule Clone()
    {
        return new PeriodSchedule() { BackupType = BackupType, TimeOfDay = TimeOfDay, DaysOfWeek = DaysOfWeek, LastRun = LastRun };
    }
}