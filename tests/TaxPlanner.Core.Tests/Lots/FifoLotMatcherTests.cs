using TaxPlanner.Core.Lots;
using static TaxPlanner.Core.Tests.TestData;

namespace TaxPlanner.Core.Tests.Lots;

public class FifoLotMatcherTests
{
    private const string I = "IE00B3XXRP09";
    private const string Other = "US7561091049";

    [Fact]
    public void Buys_without_sells_become_open_lots_with_exemption_dates()
    {
        var lot = Assert.Single(FifoLotMatcher.Match([Buy(I, "2024-03-15", 1.5m, 150.00m)]).OpenLots);
        Assert.Equal(new DateOnly(2024, 3, 15), lot.AcquiredOn);
        Assert.Equal(1.5m, lot.Quantity);
        Assert.Equal(150.00m, lot.CostEur);
        Assert.Equal(new DateOnly(2026, 3, 16), lot.ExemptFrom);
    }

    [Fact]
    public void Sell_consumes_oldest_lot_first()
    {
        var r = FifoLotMatcher.Match([
            Buy(I, "2024-01-10", 10, 100), Buy(I, "2025-01-10", 10, 200), Sell(I, "2025-06-01", 15, 450)]);

        Assert.Equal([10m, 5m], r.Disposals.Select(d => d.Quantity));
        Assert.Equal([100m, 100m], r.Disposals.Select(d => d.CostEur));
        Assert.Equal([300m, 150m], r.Disposals.Select(d => d.ProceedsEur));
        Assert.Equal([new DateOnly(2024, 1, 10), new DateOnly(2025, 1, 10)], r.Disposals.Select(d => d.AcquiredOn));
        var open = Assert.Single(r.OpenLots);
        Assert.Equal(5m, open.Quantity);
        Assert.Equal(100m, open.CostEur);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void Disposal_gain_and_exemption_are_computed()
    {
        var d = Assert.Single(FifoLotMatcher.Match([Buy(I, "2022-01-10", 1, 100), Sell(I, "2025-01-10", 1, 130)]).Disposals);
        Assert.Equal(30m, d.GainEur);
        Assert.True(d.IsExempt);
    }

    [Fact]
    public void Fractional_quantities_are_preserved_exactly()
    {
        var r = FifoLotMatcher.Match([Buy(I, "2025-01-10", 0.1234567m, 12.34567m), Sell(I, "2025-02-01", 0.0234567m, 3m)]);
        Assert.Equal(0.1m, Assert.Single(r.OpenLots).Quantity);
    }

    [Fact]
    public void Same_timestamp_rows_keep_file_order()
    {
        var r = FifoLotMatcher.Match([Sell(I, "2025-01-10", 1, 12, line: 3), Buy(I, "2025-01-10", 1, 10, line: 2)]);
        Assert.Single(r.Disposals);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void Lots_are_matched_per_isin()
    {
        var r = FifoLotMatcher.Match([Buy(I, "2025-01-10", 1, 10), Buy(Other, "2025-01-11", 1, 20), Sell(Other, "2025-02-01", 1, 25)]);
        Assert.Equal(Other, Assert.Single(r.Disposals).Isin);
        Assert.Equal(I, Assert.Single(r.OpenLots).Isin);
    }

    [Fact]
    public void Dividends_are_ignored()
        => Assert.Empty(FifoLotMatcher.Match([Dividend(I, "2025-01-10", 1m, 0m)]).OpenLots);

    [Fact]
    public void Sell_exceeding_holdings_matches_what_exists_and_warns()
    {
        var r = FifoLotMatcher.Match([Buy(I, "2025-01-10", 2, 20), Sell(I, "2025-02-01", 5, 60)]);
        var d = Assert.Single(r.Disposals);
        Assert.Equal(2m, d.Quantity);
        Assert.Equal(24m, d.ProceedsEur);
        Assert.Empty(r.OpenLots);
        Assert.Contains("history may be incomplete", Assert.Single(r.Warnings));
    }
}
