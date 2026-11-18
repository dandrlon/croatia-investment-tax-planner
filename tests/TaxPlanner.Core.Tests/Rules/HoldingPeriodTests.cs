using TaxPlanner.Core.Rules;

namespace TaxPlanner.Core.Tests.Rules;

public class HoldingPeriodTests
{
    [Theory]
    [InlineData("2024-03-15", "2026-03-16")]
    [InlineData("2024-02-29", "2026-03-01")] // leap day: AddYears gives Feb 28, plus 1 day
    public void ExemptFrom_is_day_after_two_year_anniversary(string acquired, string expected)
        => Assert.Equal(DateOnly.Parse(expected), HoldingPeriod.ExemptFrom(DateOnly.Parse(acquired)));

    [Fact]
    public void Sale_on_anniversary_is_not_exempt()
        => Assert.False(HoldingPeriod.IsExempt(new(2024, 3, 15), new(2026, 3, 15)));

    [Fact]
    public void Sale_day_after_anniversary_is_exempt()
        => Assert.True(HoldingPeriod.IsExempt(new(2024, 3, 15), new(2026, 3, 16)));
}
