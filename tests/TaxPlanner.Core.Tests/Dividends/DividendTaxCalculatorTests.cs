using TaxPlanner.Core.Dividends;
using static TaxPlanner.Core.Tests.TestData;

namespace TaxPlanner.Core.Tests.Dividends;

public class DividendTaxCalculatorTests
{
    private const string Vusa = "IE00B3XXRP09";
    private const string RealtyIncome = "US7561091049";
    private const string GeneralMills = "US3703341046";

    [Fact]
    public void Irish_etf_without_withholding_owes_full_12_percent()
    {
        var y = DividendTaxCalculator.Calculate([Dividend(Vusa, "2025-03-26", 10.00m, 0m)]).Single();
        Assert.Equal(10.00m, y.GrossEur);
        Assert.Equal(1.20m, y.CroatianTaxEur);
        Assert.Equal(0m, y.ForeignTaxCreditEur);
        Assert.Equal(1.20m, y.TaxDueEur);
    }

    [Fact]
    public void Us_withholding_of_15_percent_fully_covers_croatian_tax()
    {
        var y = DividendTaxCalculator.Calculate([Dividend(RealtyIncome, "2025-05-15", 8.50m, 1.50m)]).Single();
        Assert.Equal(10.00m, y.GrossEur);
        Assert.Equal(1.20m, y.ForeignTaxCreditEur);
        Assert.Equal(0m, y.TaxDueEur);
    }

    [Fact]
    public void Credit_is_capped_at_croatian_tax()
        => Assert.Equal(1.20m, DividendTaxCalculator.Calculate([Dividend(GeneralMills, "2025-05-01", 7.00m, 3.00m)]).Single().ForeignTaxCreditEur);

    [Fact]
    public void Partial_withholding_leaves_remainder_due()
        => Assert.Equal(0.70m, DividendTaxCalculator.Calculate([Dividend(GeneralMills, "2025-05-01", 9.50m, 0.50m)]).Single().TaxDueEur);

    [Fact]
    public void Credit_is_per_payment_not_pooled()
    {
        var y = DividendTaxCalculator.Calculate([
            Dividend(Vusa, "2025-03-26", 10.00m, 0m),
            Dividend(RealtyIncome, "2025-05-15", 7.00m, 3.00m)]).Single();
        Assert.Equal(1.20m, y.TaxDueEur);
    }

    [Fact]
    public void Rounding_happens_once_on_the_sum()
    {
        // three payments of gross 0.04 → 0.0048 each; sum 0.0144 → 0.01 (per-row rounding would give 0.00)
        var y = DividendTaxCalculator.Calculate(Enumerable.Range(1, 3)
            .Select(d => Dividend(Vusa, $"2025-01-0{d}", 0.04m, 0m))).Single();
        Assert.Equal(0.01m, y.TaxDueEur);
    }

    [Fact]
    public void Groups_by_year_and_position_in_ascending_year_order()
    {
        var years = DividendTaxCalculator.Calculate([
            Dividend(RealtyIncome, "2025-05-15", 8.50m, 1.50m),
            Dividend(Vusa, "2024-03-27", 5.00m, 0m),
            Dividend(Vusa, "2025-03-26", 10.00m, 0m),
            Dividend(Vusa, "2025-06-25", 10.00m, 0m)]);

        Assert.Equal([2024, 2025], years.Select(y => y.Year));
        Assert.Single(years[0].Positions);
        Assert.Equal(2, years[1].Positions.Count);
        Assert.Equal(20.00m, years[1].Positions.Single(p => p.Isin == Vusa).GrossEur);
    }

    [Fact]
    public void Non_dividend_transactions_are_ignored()
        => Assert.Empty(DividendTaxCalculator.Calculate([Buy(Vusa, "2025-01-10", 1m, 100m)]));

    [Fact]
    public void Year_without_configured_rate_returns_null_tax_and_warning()
    {
        var y = DividendTaxCalculator.Calculate([Dividend(Vusa, "2023-06-28", 10.00m, 0m)]).Single();
        Assert.Null(y.Rate);
        Assert.Null(y.TaxDueEur);
        Assert.Null(y.Positions.Single().TaxDueEur);
        Assert.Equal(10.00m, y.GrossEur);
        Assert.Contains("2023", Assert.Single(y.Warnings));
    }
}
