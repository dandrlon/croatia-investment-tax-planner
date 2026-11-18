using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Core.Tests;

/// <summary>Builders for transactions used across tests.</summary>
internal static class TestData
{
    private static int _line = 1;

    private static DateTimeOffset Noon(string date) =>
        new(DateOnly.Parse(date).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    public static Transaction Dividend(string isin, string date, decimal netEur, decimal withheldEur) => new()
    {
        Type = TransactionType.Dividend,
        TimeUtc = Noon(date),
        Isin = isin,
        Ticker = isin[..4],
        Name = isin,
        Quantity = 1m,
        TotalEur = netEur,
        WithholdingTaxEur = withheldEur,
        SourceHash = Guid.NewGuid().ToString("N"),
        SourceLine = Interlocked.Increment(ref _line),
    };

    public static Transaction Buy(string isin, string date, decimal quantity, decimal totalEur, int? line = null)
        => Trade(TransactionType.Buy, isin, date, quantity, totalEur, line);

    public static Transaction Sell(string isin, string date, decimal quantity, decimal totalEur, int? line = null)
        => Trade(TransactionType.Sell, isin, date, quantity, totalEur, line);

    private static Transaction Trade(TransactionType type, string isin, string date, decimal quantity, decimal totalEur, int? line) => new()
    {
        Type = type,
        TimeUtc = Noon(date),
        Isin = isin,
        Ticker = isin[..4],
        Name = isin,
        Quantity = quantity,
        TotalEur = totalEur,
        SourceHash = Guid.NewGuid().ToString("N"),
        SourceLine = line ?? Interlocked.Increment(ref _line),
    };
}
