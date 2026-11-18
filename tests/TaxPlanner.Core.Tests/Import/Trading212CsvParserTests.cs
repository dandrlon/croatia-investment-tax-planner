using System.Globalization;
using System.Text;
using TaxPlanner.Core.Import;
using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Core.Tests.Import;

public class Trading212CsvParserTests
{
    private const string Header =
        "Action,Time,ISIN,Ticker,Name,ID,No. of shares,Price / share,Currency (Price / share),Exchange rate,Total,Currency (Total),Withholding tax,Currency (Withholding tax)";

    private static ParseResult Parse(string csv) => Trading212CsvParser.Parse(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

    private static string Row(
        string action = "Market buy", string time = "2025-01-10 10:00:00", string shares = "1",
        string exchangeRate = "1.08", string total = "100.00", string totalCurrency = "EUR",
        string withholding = "", string withholdingCurrency = "", string id = "EOF1")
        => $"{Header}\n{action},{time},US7561091049,O,Realty Income,{id},{shares},50.00,USD,{exchangeRate},{total},{totalCurrency},{withholding},{withholdingCurrency}\n";

    private static ParseResult Sample()
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-export.csv"));
        return Trading212CsvParser.Parse(file);
    }

    [Fact]
    public void Parses_sample_fixture()
    {
        var r = Sample();
        Assert.Empty(r.Errors);
        Assert.Equal(6, r.Transactions.Count);
        Assert.Equal(3, r.Transactions.Count(t => t.Type == TransactionType.Buy));
        Assert.Equal(1, r.Transactions.Count(t => t.Type == TransactionType.Sell));
        Assert.Equal(2, r.Transactions.Count(t => t.Type == TransactionType.Dividend));
        Assert.Equal(["Deposit", "Interest on cash"], r.Skipped.Select(s => s.Action));
        Assert.Equal([2, 8], r.Skipped.Select(s => s.Line));
        Assert.All(r.Skipped, s => Assert.Equal("Not a trade or dividend", s.Reason));
    }

    [Fact]
    public void Maps_buy_fields()
    {
        var t = Sample().Transactions.First();
        Assert.Equal(TransactionType.Buy, t.Type);
        Assert.Equal(new DateTimeOffset(2024, 3, 15, 10, 15, 30, TimeSpan.Zero), t.TimeUtc);
        Assert.Equal("IE00B3XXRP09", t.Isin);
        Assert.Equal("VUSA", t.Ticker);
        Assert.Equal(1.5m, t.Quantity);
        Assert.Equal(129.18m, t.TotalEur);
        Assert.Equal(3, t.SourceLine);
    }

    [Fact]
    public void Name_with_comma_is_read_intact()
        => Assert.Contains(Sample().Transactions, t => t.Name == "General Mills, Inc.");

    [Fact]
    public void Us_dividend_withholding_converted_with_exchange_rate()
    {
        var t = Parse(Row(action: "Dividend (Dividends paid by us corporations)", withholding: "1.62", withholdingCurrency: "USD")).Transactions.Single();
        Assert.Equal(TransactionType.Dividend, t.Type);
        Assert.Equal(1.62m, t.WithholdingTax);
        Assert.Equal("USD", t.WithholdingTaxCurrency);
        Assert.Equal(1.50m, t.WithholdingTaxEur);
    }

    [Fact]
    public void Eur_withholding_is_taken_as_is()
        => Assert.Equal(0.30m, Parse(Row(action: "Dividend (Ordinary)", withholding: "0.30", withholdingCurrency: "EUR")).Transactions.Single().WithholdingTaxEur);

    [Fact]
    public void Negative_withholding_is_treated_as_positive()
        => Assert.Equal(0.30m, Parse(Row(action: "Dividend (Ordinary)", withholding: "-0.30", withholdingCurrency: "EUR")).Transactions.Single().WithholdingTaxEur);

    [Fact]
    public void Empty_withholding_is_zero()
        => Assert.Equal(0m, Parse(Row(action: "Dividend (Dividend)")).Transactions.Single().WithholdingTaxEur);

    [Fact]
    public void Non_eur_withholding_without_exchange_rate_is_a_row_error()
    {
        var r = Parse(Row(action: "Dividend (Dividend)", withholding: "1.00", withholdingCurrency: "USD", exchangeRate: ""));
        Assert.Empty(r.Transactions);
        Assert.Equal(2, Assert.Single(r.Errors).Line);
    }

    [Theory]
    [InlineData("Market buy", TransactionType.Buy)]
    [InlineData("Limit buy", TransactionType.Buy)]
    [InlineData("Stop limit buy", TransactionType.Buy)]
    [InlineData("Market sell", TransactionType.Sell)]
    [InlineData("LIMIT SELL", TransactionType.Sell)]
    [InlineData("Dividend (Ordinary)", TransactionType.Dividend)]
    public void Maps_actions(string action, TransactionType expected)
        => Assert.Equal(expected, Parse(Row(action: action)).Transactions.Single().Type);

    [Fact]
    public void Missing_required_column_rejects_file()
    {
        var csv = "Action,Time,ISIN,Ticker,Name,No. of shares,Currency (Total)\nMarket buy,2025-01-10 10:00:00,X,X,X,1,EUR\n";
        var r = Parse(csv);
        Assert.Equal([new ParseError(1, "Missing required column 'Total'")], r.Errors);
        Assert.Empty(r.Transactions);
    }

    [Fact]
    public void Empty_file_is_an_error()
        => Assert.Equal(1, Assert.Single(Parse("").Errors).Line);

    [Fact]
    public void Non_eur_total_is_a_row_error()
    {
        var csv = Row(totalCurrency: "GBP")
            + "Market buy,2025-01-11 10:00:00,US7561091049,O,Realty Income,EOF2,1,50.00,USD,1.08,46.30,EUR,,\n";
        var r = Parse(csv);
        var error = Assert.Single(r.Errors);
        Assert.Equal(2, error.Line);
        Assert.Contains("EUR", error.Message);
        Assert.Equal(3, Assert.Single(r.Transactions).SourceLine);
    }

    [Fact]
    public void Unparseable_number_is_a_row_error()
        => Assert.Equal(2, Assert.Single(Parse(Row(shares: "abc")).Errors).Line);

    [Fact]
    public void Columns_in_different_order_still_map()
    {
        var csv = "Total,Currency (Total),No. of shares,Name,Ticker,ISIN,Time,Action\n"
            + "100.00,EUR,2,Realty Income,O,US7561091049,2025-01-10 10:00:00,Market buy\n";
        var t = Parse(csv).Transactions.Single();
        Assert.Equal(2m, t.Quantity);
        Assert.Equal(100.00m, t.TotalEur);
        Assert.Equal("O", t.Ticker);
    }

    [Fact]
    public void Time_with_milliseconds_parses_as_utc()
        => Assert.Equal(new DateTimeOffset(2025, 1, 10, 23, 30, 0, 123, TimeSpan.Zero),
            Parse(Row(time: "2025-01-10 23:30:00.123")).Transactions.Single().TimeUtc);

    [Fact]
    public void Source_hash_is_stable_and_uses_id_when_present()
    {
        var a = Parse(Row(id: "EOF1")).Transactions.Single().SourceHash;
        Assert.Equal(a, Parse(Row(id: "EOF1", total: "999.00")).Transactions.Single().SourceHash);
        Assert.NotEqual(a, Parse(Row(id: "EOF2")).Transactions.Single().SourceHash);
        Assert.Matches("^[0-9a-f]{64}$", a);
    }

    [Fact]
    public void Source_hash_without_id_uses_row_content()
    {
        var a = Parse(Row(id: "")).Transactions.Single().SourceHash;
        Assert.Equal(a, Parse(Row(id: "")).Transactions.Single().SourceHash);
        Assert.NotEqual(a, Parse(Row(id: "", total: "101.00")).Transactions.Single().SourceHash);
    }

    [Fact]
    public void Parses_numbers_invariantly_under_croatian_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("hr-HR");
        try
        {
            var t = Parse(Row(shares: "12.5", total: "1234.56")).Transactions.Single();
            Assert.Equal(12.5m, t.Quantity);
            Assert.Equal(1234.56m, t.TotalEur);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Withholding_in_a_currency_other_than_eur_or_the_price_currency_is_a_row_error()
    {
        // The exchange rate is for the price currency (USD), so it cannot convert GBP.
        var r = Parse(Row(action: "Dividend (Ordinary)", withholding: "1.00", withholdingCurrency: "GBP"));
        Assert.Empty(r.Transactions);
        Assert.Contains("GBP", Assert.Single(r.Errors).Message);
    }

    [Fact]
    public void Withholding_without_a_currency_is_a_row_error()
        => Assert.Single(Parse(Row(action: "Dividend (Ordinary)", withholding: "1.00", withholdingCurrency: "")).Errors);

    [Theory]
    [InlineData("Market buy", "0")]
    [InlineData("Market buy", "-1")]
    [InlineData("Market sell", "0")]
    [InlineData("Market sell", "-2")]
    public void Trade_with_non_positive_quantity_is_a_row_error(string action, string shares)
    {
        var r = Parse(Row(action: action, shares: shares));
        Assert.Empty(r.Transactions);
        Assert.Equal(2, Assert.Single(r.Errors).Line);
    }

    [Fact]
    public void Negative_total_is_a_row_error()
        => Assert.Single(Parse(Row(total: "-100.00")).Errors);

    [Fact]
    public void Thousands_separator_is_rejected_instead_of_misread()
    {
        // "12,5" from a Croatian spreadsheet must not silently become 125.
        var r = Parse(Row(shares: "\"12,5\""));
        Assert.Empty(r.Transactions);
        Assert.Single(r.Errors);
    }
}
