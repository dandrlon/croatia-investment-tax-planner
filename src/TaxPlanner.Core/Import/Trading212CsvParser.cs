using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Core.Import;

public record ParseResult(IReadOnlyList<Transaction> Transactions, IReadOnlyList<SkippedRow> Skipped, IReadOnlyList<ParseError> Errors);

/// <summary>A row that was read but is not a buy, sell or dividend (deposits, interest, ...).</summary>
public record SkippedRow(int Line, string Action, string Reason);

/// <summary>A problem in the file. Line 1 is the header; line 1 errors reject the whole file.</summary>
public record ParseError(int Line, string Message);

/// <summary>
/// Reads a Trading 212 "History" CSV export. Columns are found by header name because
/// Trading 212 leaves out columns it has no data for. All numbers use the invariant
/// culture (a dot as decimal separator), whatever the machine's regional settings are.
/// </summary>
public static class Trading212CsvParser
{
    private static readonly string[] RequiredColumns =
        ["Action", "Time", "ISIN", "Ticker", "Name", "No. of shares", "Total", "Currency (Total)"];

    private static readonly string[] TimeFormats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff"];

    public static ParseResult Parse(Stream csv)
    {
        var transactions = new List<Transaction>();
        var skipped = new List<SkippedRow>();
        var errors = new List<ParseError>();

        using var reader = new StreamReader(csv);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null });

        if (!csvReader.Read() || !csvReader.ReadHeader() || csvReader.HeaderRecord is not { Length: > 0 } header)
            return new ParseResult([], [], [new ParseError(1, "The file is empty.")]);

        var missing = RequiredColumns.FirstOrDefault(c => !header.Contains(c));
        if (missing is not null)
            return new ParseResult([], [], [new ParseError(1, $"Missing required column '{missing}'")]);

        while (csvReader.Read())
        {
            var row = new Row(csvReader, header);
            var line = csvReader.Parser.RawRow;
            try
            {
                var type = MapAction(row.Text("Action"));
                if (type is null)
                    skipped.Add(new SkippedRow(line, row.Text("Action"), "Not a trade or dividend"));
                else
                    transactions.Add(ToTransaction(row, type.Value, line));
            }
            catch (FormatException ex)
            {
                errors.Add(new ParseError(line, ex.Message));
            }
        }

        return new ParseResult(transactions, skipped, errors);
    }

    private static TransactionType? MapAction(string action)
    {
        if (action.EndsWith("buy", StringComparison.OrdinalIgnoreCase)) return TransactionType.Buy;
        if (action.EndsWith("sell", StringComparison.OrdinalIgnoreCase)) return TransactionType.Sell;
        if (action.StartsWith("Dividend", StringComparison.OrdinalIgnoreCase)) return TransactionType.Dividend;
        return null;
    }

    private static Transaction ToTransaction(Row row, TransactionType type, int line)
    {
        var totalCurrency = row.Text("Currency (Total)");
        if (!string.Equals(totalCurrency, "EUR", StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"Total is in '{totalCurrency}'. Only EUR accounts are supported in this version.");

        var quantity = row.RequiredNumber("No. of shares");
        if (type is TransactionType.Buy or TransactionType.Sell && quantity <= 0)
            throw new FormatException($"No. of shares must be greater than zero for a {type.ToString().ToLowerInvariant()}, but is {quantity}.");

        var total = row.RequiredNumber("Total");
        if (total < 0)
            throw new FormatException($"Total must not be negative, but is {total}.");

        var exchangeRate = row.Number("Exchange rate");
        var priceCurrency = row.OptionalText("Currency (Price / share)");
        var withholding = row.Number("Withholding tax") is { } w ? Math.Abs(w) : (decimal?)null;
        var withholdingCurrency = row.OptionalText("Currency (Withholding tax)");

        return new Transaction
        {
            Type = type,
            TimeUtc = row.Time("Time"),
            Isin = row.Text("ISIN"),
            Ticker = row.Text("Ticker"),
            Name = row.Text("Name"),
            Quantity = quantity,
            PricePerShare = row.Number("Price / share"),
            PriceCurrency = priceCurrency,
            ExchangeRate = exchangeRate,
            TotalEur = total,
            WithholdingTax = withholding,
            WithholdingTaxCurrency = withholdingCurrency,
            WithholdingTaxEur = WithholdingToEur(withholding, withholdingCurrency, priceCurrency, exchangeRate),
            SourceHash = Hash(row),
            SourceLine = line,
        };
    }

    /// <summary>
    /// Converts withholding tax to EUR. The row's exchange rate is units of the price currency
    /// per 1 EUR, so it can only convert an amount in that same currency.
    /// </summary>
    private static decimal WithholdingToEur(decimal? amount, string? currency, string? priceCurrency, decimal? exchangeRate)
    {
        if (amount is null or 0) return 0;
        if (currency is null)
            throw new FormatException("Withholding tax has no currency.");
        if (string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase)) return amount.Value;
        if (!string.Equals(currency, priceCurrency, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"Withholding tax is in '{currency}' but the exchange rate is for '{priceCurrency}', so it cannot be converted to EUR.");
        if (exchangeRate is null or 0)
            throw new FormatException($"Withholding tax is in '{currency}' but the row has no exchange rate to convert it to EUR.");
        return amount.Value / exchangeRate.Value;
    }

    private static string Hash(Row row)
    {
        var id = row.OptionalText("ID");
        var key = id is not null
            ? "id:" + id
            : string.Join('|', row.Text("Action"), row.Text("Time"), row.Text("ISIN"), row.Text("No. of shares"), row.Text("Total"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    /// <summary>Reads fields of the current CSV row by column name.</summary>
    private sealed class Row(CsvReader reader, string[] header)
    {
        public string Text(string column) => OptionalText(column) ?? "";

        public string? OptionalText(string column)
        {
            var index = Array.IndexOf(header, column);
            if (index < 0) return null;
            var value = reader.GetField(index)?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public decimal? Number(string column)
        {
            var text = OptionalText(column);
            if (text is null) return null;
            // No thousands separator: a "12,5" from a Croatian spreadsheet must fail, not become 125.
            const NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            if (decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out var value))
                return value;
            throw new FormatException($"'{text}' in column '{column}' is not a number.");
        }

        public decimal RequiredNumber(string column)
            => Number(column) ?? throw new FormatException($"Column '{column}' is empty.");

        public DateTimeOffset Time(string column)
        {
            var text = Text(column);
            if (DateTime.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc))
                return new DateTimeOffset(utc, TimeSpan.Zero);
            throw new FormatException($"'{text}' in column '{column}' is not a valid time.");
        }
    }
}
