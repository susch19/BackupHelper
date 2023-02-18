namespace BackupService.Scheduling;

[Flags]
public enum DaysOfWeek
{
    None = 0,
    //
    // Summary:
    //     Indicates Sunday.
    Sunday = 1 << 0,
    //
    // Summary:
    //     Indicates Monday.
    Monday = 1 << 1,
    //
    // Summary:
    //     Indicates Tuesday.
    Tuesday = 1 << 2,
    //
    // Summary:
    //     Indicates Wednesday.
    Wednesday = 1 << 3,
    //
    // Summary:
    //     Indicates Thursday.
    Thursday = 1 << 4,
    //
    // Summary:
    //     Indicates Friday.
    Friday = 1 << 5,
    //
    // Summary:
    //     Indicates Saturday.
    Saturday = 1 << 6,

    Workdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}
