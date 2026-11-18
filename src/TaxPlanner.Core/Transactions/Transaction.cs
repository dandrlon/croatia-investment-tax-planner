namespace TaxPlanner.Core.Transactions;

public enum TransactionType
{
    Buy,
    Sell,
    Dividend,
}

/// <summary>
/// One buy, sell or dividend row from a Trading 212 export.
/// Amounts ending in "Eur" are in the account currency (EUR).
/// </summary>
public class Transaction
{
    public int Id { get; init; }
    public int ImportBatchId { get; init; }
    public TransactionType Type { get; init; }
    public DateTimeOffset TimeUtc { get; init; }
    public string Isin { get; init; } = "";
    public string Ticker { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>Shares bought or sold; for dividends, the shares the dividend was paid on.</summary>
    public decimal Quantity { get; init; }

    public decimal? PricePerShare { get; init; }
    public string? PriceCurrency { get; init; }

    /// <summary>Units of instrument currency per 1 EUR, as reported by Trading 212.</summary>
    public decimal? ExchangeRate { get; init; }

    /// <summary>
    /// Buy: total cost. Sell: net proceeds. Dividend: net amount received after withholding.
    /// </summary>
    public decimal TotalEur { get; init; }

    /// <summary>Withholding tax in its original currency (dividends only).</summary>
    public decimal? WithholdingTax { get; init; }
    public string? WithholdingTaxCurrency { get; init; }
    public decimal WithholdingTaxEur { get; init; }

    /// <summary>Fingerprint of the source row, used to skip rows that were already imported.</summary>
    public string SourceHash { get; init; } = "";

    /// <summary>Line number in the source file; breaks ties between rows with the same timestamp.</summary>
    public int SourceLine { get; init; }
}
