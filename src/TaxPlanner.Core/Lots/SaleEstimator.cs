using TaxPlanner.Core.Rules;

namespace TaxPlanner.Core.Lots;

public record SaleEstimate(
    DateOnly AsOf,
    decimal? Rate,
    decimal RealizedTaxableGainEur,
    decimal HypotheticalTaxableGainEur,
    decimal? EstimatedTaxEur,
    IReadOnlyList<PositionEstimate> Positions,
    IReadOnlyList<string> Warnings);

/// <param name="TaxableGainEur">Gain on lots held two years or less; negative for a loss.</param>
/// <param name="NextExemptionDate">The earliest future date on which another lot becomes exempt.</param>
public record PositionEstimate(
    string Isin,
    string Ticker,
    decimal Quantity,
    decimal MarketValueEur,
    decimal ExemptGainEur,
    decimal TaxableGainEur,
    DateOnly? NextExemptionDate);

/// <summary>
/// Estimates the capital gains tax for the current year if every open position were sold today.
/// Gains and losses on non-exempt disposals in the same calendar year offset each other,
/// including sales already made this year. Exempt lots stay outside the calculation.
/// </summary>
public static class SaleEstimator
{
    public static SaleEstimate Estimate(LotMatchResult lots, IReadOnlyDictionary<string, decimal> pricesEurByIsin, DateOnly today)
    {
        // Keep the lot matcher's warnings: an incomplete history makes this estimate incomplete too.
        var warnings = new List<string>(lots.Warnings);
        var positions = new List<PositionEstimate>();

        foreach (var position in lots.OpenLots.GroupBy(l => l.Isin))
        {
            var first = position.First();
            if (!pricesEurByIsin.TryGetValue(position.Key, out var price))
            {
                warnings.Add($"No current price given for {first.Ticker} ({first.Isin}); it is excluded from the estimate.");
                continue;
            }

            decimal exempt = 0, taxable = 0;
            foreach (var lot in position)
            {
                var gain = price * lot.Quantity - lot.CostEur;
                if (HoldingPeriod.IsExempt(lot.AcquiredOn, today)) exempt += gain;
                else taxable += gain;
            }

            var next = position.Select(l => l.ExemptFrom).Where(d => d > today).DefaultIfEmpty().Min();
            positions.Add(new PositionEstimate(first.Isin, first.Ticker, position.Sum(l => l.Quantity),
                Money.Round(price * position.Sum(l => l.Quantity)), Money.Round(exempt), Money.Round(taxable),
                next == default ? null : next));
        }

        var realized = lots.Disposals
            .Where(d => d.DisposedOn.Year == today.Year && !d.IsExempt)
            .Sum(d => d.GainEur);
        var hypothetical = lots.OpenLots
            .Where(l => pricesEurByIsin.ContainsKey(l.Isin) && !HoldingPeriod.IsExempt(l.AcquiredOn, today))
            .Sum(l => pricesEurByIsin[l.Isin] * l.Quantity - l.CostEur);

        var rate = TaxRates.CapitalIncomeRate(today.Year);
        decimal? tax = rate is null ? null : Money.Round(rate.Value * Math.Max(0, realized + hypothetical));
        if (rate is null)
            warnings.Add($"No verified tax rate is configured for {today.Year}. Tax is not calculated.");

        return new SaleEstimate(today, rate, Money.Round(realized), Money.Round(hypothetical), tax, positions, warnings);
    }
}
