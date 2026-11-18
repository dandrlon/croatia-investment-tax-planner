using TaxPlanner.Core.Rules;

namespace TaxPlanner.Core.Tests.Rules;

public class CroatianCalendarTests
{
    [Fact]
    public void Late_utc_evening_on_new_years_eve_is_next_day_in_zagreb()
        => Assert.Equal(new DateOnly(2025, 1, 1),
            CroatianCalendar.ToLocalDate(new DateTimeOffset(2024, 12, 31, 23, 30, 0, TimeSpan.Zero)));

    [Fact]
    public void Summer_time_offset_is_two_hours()
        => Assert.Equal(new DateOnly(2025, 7, 2),
            CroatianCalendar.ToLocalDate(new DateTimeOffset(2025, 7, 1, 22, 30, 0, TimeSpan.Zero)));
}
