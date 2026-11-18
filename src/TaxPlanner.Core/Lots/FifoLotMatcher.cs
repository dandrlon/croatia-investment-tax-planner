using TaxPlanner.Core.Rules;
using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Core.Lots;

/// <summary>Shares from one purchase that are still held.</summary>
public record OpenLot(string Isin, string Ticker, string Name, DateOnly AcquiredOn, decimal Quantity, decimal CostEur)
{
    public DateOnly ExemptFrom => HoldingPeriod.ExemptFrom(AcquiredOn);
}

/// <summary>Part of a sale matched to one purchase lot.</summary>
public record Disposal(string Isin, DateOnly AcquiredOn, DateOnly DisposedOn, decimal Quantity, decimal CostEur, decimal ProceedsEur)
{
    public decimal GainEur => ProceedsEur - CostEur;
    public bool IsExempt => HoldingPeriod.IsExempt(AcquiredOn, DisposedOn);
}

public record LotMatchResult(IReadOnlyList<OpenLot> OpenLots, IReadOnlyList<Disposal> Disposals, IReadOnlyList<string> Warnings);

/// <summary>
/// Matches sells to buys first-in, first-out, per ISIN. When a sale uses only part of a lot,
/// cost and proceeds are split in proportion to the quantity.
/// </summary>
public static class FifoLotMatcher
{
    public static LotMatchResult Match(IEnumerable<Transaction> transactions)
    {
        var openLots = new List<OpenLot>();
        var disposals = new List<Disposal>();
        var warnings = new List<string>();

        var trades = transactions
            .Where(t => t.Type is TransactionType.Buy or TransactionType.Sell)
            .OrderBy(t => t.TimeUtc)
            .ThenBy(t => t.SourceLine);

        foreach (var byIsin in trades.GroupBy(t => t.Isin))
        {
            var lots = new List<OpenLot>(); // oldest first
            foreach (var trade in byIsin)
            {
                var date = CroatianCalendar.ToLocalDate(trade.TimeUtc);
                if (trade.Type == TransactionType.Buy)
                    lots.Add(new OpenLot(trade.Isin, trade.Ticker, trade.Name, date, trade.Quantity, trade.TotalEur));
                else
                    Sell(trade, date, lots, disposals, warnings);
            }

            openLots.AddRange(lots);
        }

        return new LotMatchResult(openLots, disposals, warnings);
    }

    private static void Sell(Transaction sale, DateOnly date, List<OpenLot> lots, List<Disposal> disposals, List<string> warnings)
    {
        var remaining = sale.Quantity;
        while (remaining > 0 && lots.Count > 0)
        {
            var lot = lots[0];
            var quantity = Math.Min(remaining, lot.Quantity);
            var cost = lot.CostEur * quantity / lot.Quantity;
            var proceeds = sale.TotalEur * quantity / sale.Quantity;
            disposals.Add(new Disposal(sale.Isin, lot.AcquiredOn, date, quantity, cost, proceeds));

            if (quantity < lot.Quantity)
                lots[0] = lot with { Quantity = lot.Quantity - quantity, CostEur = lot.CostEur - cost };
            else
                lots.RemoveAt(0);

            remaining -= quantity;
        }

        if (remaining > 0)
            warnings.Add($"Sell of {sale.Quantity} {sale.Ticker} on {date:yyyy-MM-dd} exceeds held shares by {remaining}; the import history may be incomplete.");
    }
}
