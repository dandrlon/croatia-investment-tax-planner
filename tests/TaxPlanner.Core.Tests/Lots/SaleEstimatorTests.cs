using TaxPlanner.Core.Lots;

namespace TaxPlanner.Core.Tests.Lots;

public class SaleEstimatorTests
{
    private const string A = "IE00B3XXRP09";
    private const string B = "US7561091049";
    private static readonly DateOnly Today = new(2026, 11, 18);

    private static OpenLot Lot(string isin, string acquired, decimal qty, decimal cost)
        => new(isin, isin[..4], isin, DateOnly.Parse(acquired), qty, cost);

    private static SaleEstimate Estimate(IEnumerable<OpenLot> lots, Dictionary<string, decimal> prices, IEnumerable<Disposal>? disposals = null)
        => SaleEstimator.Estimate(new LotMatchResult(lots.ToList(), (disposals ?? []).ToList(), []), prices, Today);

    [Fact]
    public void Exempt_lots_contribute_no_tax()
    {
        var e = Estimate([Lot(A, "2023-01-10", 10, 100)], new() { [A] = 30 });
        var p = Assert.Single(e.Positions);
        Assert.Equal(300m, p.MarketValueEur);
        Assert.Equal(200m, p.ExemptGainEur);
        Assert.Equal(0m, p.TaxableGainEur);
        Assert.Equal(0m, e.EstimatedTaxEur);
    }

    [Fact]
    public void Non_exempt_gain_taxed_at_12_percent()
    {
        var e = Estimate([Lot(A, "2025-06-01", 10, 100)], new() { [A] = 15 });
        Assert.Equal(50m, Assert.Single(e.Positions).TaxableGainEur);
        Assert.Equal(50m, e.HypotheticalTaxableGainEur);
        Assert.Equal(0.12m, e.Rate);
        Assert.Equal(6.00m, e.EstimatedTaxEur);
    }

    [Fact]
    public void Losses_offset_gains_across_positions_in_same_year()
    {
        var e = Estimate([Lot(A, "2025-06-01", 10, 100), Lot(B, "2025-06-01", 10, 100)], new() { [A] = 15, [B] = 7 });
        Assert.Equal(20m, e.HypotheticalTaxableGainEur);
        Assert.Equal(2.40m, e.EstimatedTaxEur);
    }

    [Fact]
    public void Net_loss_gives_zero_tax_not_negative()
    {
        var e = Estimate([Lot(A, "2025-06-01", 10, 100)], new() { [A] = 7 });
        Assert.Equal(-30m, Assert.Single(e.Positions).TaxableGainEur);
        Assert.Equal(0m, e.EstimatedTaxEur);
    }

    [Fact]
    public void Loss_on_exempt_lot_does_not_offset_taxable_gain()
    {
        // exempt lot: 150 - 180 = -30 (outside the tax); taxable lot: 150 - 100 = +50
        var mixed = Estimate([Lot(A, "2023-01-10", 10, 180), Lot(A, "2025-06-01", 10, 100)], new() { [A] = 15 });
        Assert.Equal(-30m, mixed.Positions.Single().ExemptGainEur);
        Assert.Equal(50m, mixed.Positions.Single().TaxableGainEur);
        Assert.Equal(6.00m, mixed.EstimatedTaxEur);
    }

    [Fact]
    public void Realized_gains_this_year_are_included()
    {
        var disposals = new[]
        {
            new Disposal(A, new(2026, 1, 5), new(2026, 2, 1), 1, 100, 200),   // non-exempt, this year: +100
            new Disposal(A, new(2025, 1, 5), new(2025, 2, 1), 1, 100, 500),   // previous year: ignored
            new Disposal(A, new(2023, 1, 5), new(2026, 3, 1), 1, 100, 900),   // exempt: ignored
        };
        var e = Estimate([Lot(B, "2025-06-01", 10, 100)], new() { [B] = 6 }, disposals);
        Assert.Equal(100m, e.RealizedTaxableGainEur);
        Assert.Equal(-40m, e.HypotheticalTaxableGainEur);
        Assert.Equal(7.20m, e.EstimatedTaxEur);
    }

    [Fact]
    public void Missing_price_excludes_position_and_warns()
    {
        var e = Estimate([Lot(A, "2025-06-01", 1, 10), Lot(B, "2025-06-01", 1, 10)], new() { [A] = 11 });
        Assert.Equal(A, Assert.Single(e.Positions).Isin);
        Assert.Contains(B[..4], Assert.Single(e.Warnings));
    }

    [Fact]
    public void Next_exemption_date_is_earliest_future_one()
    {
        var e = Estimate([Lot(A, "2023-01-10", 1, 10), Lot(A, "2025-08-01", 1, 10), Lot(A, "2025-03-01", 1, 10)], new() { [A] = 11 });
        Assert.Equal(new DateOnly(2027, 3, 2), e.Positions.Single().NextExemptionDate);
        Assert.Equal(3m, e.Positions.Single().Quantity);
        Assert.Equal(Today, e.AsOf);
    }

    [Fact]
    public void Fully_exempt_position_has_no_next_exemption_date()
        => Assert.Null(Estimate([Lot(A, "2023-01-10", 1, 10)], new() { [A] = 11 }).Positions.Single().NextExemptionDate);

    [Fact]
    public void Lot_matching_warnings_are_carried_into_the_estimate()
    {
        var lots = new LotMatchResult([Lot(A, "2025-06-01", 1, 10)], [], ["history may be incomplete"]);
        Assert.Contains("history may be incomplete", SaleEstimator.Estimate(lots, new Dictionary<string, decimal> { [A] = 11 }, Today).Warnings);
    }
}
