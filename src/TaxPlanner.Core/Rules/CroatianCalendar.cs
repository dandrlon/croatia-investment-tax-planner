namespace TaxPlanner.Core.Rules;

/// <summary>
/// Converts Trading 212 timestamps (UTC) to the calendar date in Croatia,
/// which decides the tax year and the acquisition date of a lot.
/// </summary>
public static class CroatianCalendar
{
    private static readonly TimeZoneInfo Zagreb = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zagreb");

    public static DateOnly ToLocalDate(DateTimeOffset utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Zagreb).DateTime);
}
