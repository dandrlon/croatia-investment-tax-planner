namespace TaxPlanner.Core.Rules;

public static class TaxRates
{
    /// <summary>
    /// Croatian tax rate on capital income (dividends and capital gains) for a calendar year,
    /// or null when no verified rate is configured. Earlier years are deliberately not
    /// guessed: see docs/tax-rules.md.
    /// </summary>
    public static decimal? CapitalIncomeRate(int year) => year >= 2024 ? 0.12m : null;
}
