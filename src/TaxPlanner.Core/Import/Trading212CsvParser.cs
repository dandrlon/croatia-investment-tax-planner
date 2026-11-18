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

        var exchangeRate = row.Number("Exchange rate");
        var withholding = row.Number("Withholding tax") is { } w ? Math.Abs(w) : (decimal?)null;
        var withholdingCurrency = row.OptionalText("Currency (Withholding tax)");

        return new Transaction
        {
            Type = type,
            TimeUtc = row.Time("Time"),
            Isin = row.Text("ISIN"),
            Ticker = row.Text("Ticker"),
            Name = row.Text("Name"),
            Quantity = row.RequiredNumber("No. of shares"),
            PricePerShare = row.Number("Price / share"),
            PriceCurrency = row.OptionalText("Currency (Price / share)"),
            ExchangeRate = exchangeRate,
            TotalEur = row.RequiredNumber("Total"),
            WithholdingTax = withholding,
            WithholdingTaxCurrency = withholdingCurrency,
            WithholdingTaxEur = ToEur(withholding, withholdingCurrency, exchangeRate),
            SourceHash = Hash(row),
            SourceLine = line,
        };
    }

    /// <summary>Trading 212's exchange rate is units of instrument currency per 1 EUR.</summary>
    private static decimal ToEur(decimal? amount, string? currency, decimal? exchangeRate)
    {
        if (amount is null or 0) return 0;
        if (string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase)) return amount.Value;
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
            if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
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
