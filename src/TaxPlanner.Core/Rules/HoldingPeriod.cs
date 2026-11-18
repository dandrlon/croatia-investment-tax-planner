namespace TaxPlanner.Core.Rules;

/// <summary>
/// Capital gains on financial assets held for more than two years are exempt.
/// "More than two years" means the sale happens after the second anniversary
/// of the acquisition date, so the first exempt day is the day after it.
/// </summary>
public static class HoldingPeriod
{
    public static DateOnly ExemptFrom(DateOnly acquiredOn) => acquiredOn.AddYears(2).AddDays(1);

    public static bool IsExempt(DateOnly acquiredOn, DateOnly disposedOn) => disposedOn >= ExemptFrom(acquiredOn);
}
