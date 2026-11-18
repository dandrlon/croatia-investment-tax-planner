using System.Net;
using System.Net.Http.Json;
using TaxPlanner.Core;

namespace TaxPlanner.Api.Tests;

[Collection(ApiCollection.Name)]
public class ReportsApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Vusa = "IE00B3XXRP09";

    private record DividendPosition(string Isin, decimal GrossEur, decimal? TaxDueEur);
    private record DividendYear(int Year, decimal? Rate, decimal GrossEur, decimal WithheldEur, decimal? CroatianTaxEur,
        decimal? ForeignTaxCreditEur, decimal? TaxDueEur, List<DividendPosition> Positions, List<string> Warnings);
    private record DividendsResponse(string Disclaimer, List<DividendYear> Years);

    private record Lot(DateOnly AcquiredOn, decimal Quantity, decimal CostEur, DateOnly ExemptFrom, bool IsExemptToday);
    private record Position(string Isin, string Ticker, string Name, decimal Quantity, decimal CostEur, List<Lot> Lots);
    private record PositionsResponse(string Disclaimer, List<Position> Positions, List<string> Warnings);

    private record PositionEstimate(string Isin, decimal Quantity, decimal MarketValueEur, decimal ExemptGainEur, decimal TaxableGainEur);
    private record Estimate(DateOnly AsOf, decimal? Rate, decimal HypotheticalTaxableGainEur, decimal? EstimatedTaxEur,
        List<PositionEstimate> Positions, List<string> Warnings);
    private record EstimateResponse(string Disclaimer, Estimate Estimate);

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task ImportAsync(string csv)
        => (await _client.PostAsync("/api/imports", ApiFactory.CsvUpload(csv), Ct)).EnsureSuccessStatusCode();

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await _client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(Ct))!;
    }

    [Fact]
    public async Task Dividends_after_sample_import_match_calculator()
    {
        await ImportAsync(ApiFactory.SampleCsv);
        var r = await GetAsync<DividendsResponse>("/api/dividends");

        Assert.Equal(Disclaimer.Text, r.Disclaimer);
        var y = Assert.Single(r.Years);
        // VUSA: gross 0.35, no withholding. O: net 0.62 + 0.12 USD / 1.08 = 0.7311 gross.
        Assert.Equal(2024, y.Year);
        Assert.Equal(1.08m, y.GrossEur);
        Assert.Equal(0.11m, y.WithheldEur);
        Assert.Equal(0.13m, y.CroatianTaxEur);
        Assert.Equal(0.09m, y.ForeignTaxCreditEur);
        Assert.Equal(0.04m, y.TaxDueEur);
        Assert.Equal(2, y.Positions.Count);
    }

    [Fact]
    public async Task Dividends_year_filter_returns_only_that_year()
    {
        await ImportAsync(ApiFactory.SampleCsv);
        Assert.Single((await GetAsync<DividendsResponse>("/api/dividends?year=2024")).Years);
        Assert.Empty((await GetAsync<DividendsResponse>("/api/dividends?year=2025")).Years);
    }

    [Fact]
    public async Task Dividends_with_no_data_return_empty_list()
        => Assert.Empty((await GetAsync<DividendsResponse>("/api/dividends")).Years);

    [Fact]
    public async Task Positions_list_lots_with_exemption_dates()
    {
        await ImportAsync(ApiFactory.SampleCsv);
        var r = await GetAsync<PositionsResponse>("/api/positions");

        Assert.Equal(Disclaimer.Text, r.Disclaimer);
        Assert.Equal(3, r.Positions.Count);
        var vusa = r.Positions.Single(p => p.Isin == Vusa);
        var lot = Assert.Single(vusa.Lots);
        Assert.Equal(new DateOnly(2024, 3, 15), lot.AcquiredOn);
        Assert.Equal(new DateOnly(2026, 3, 16), lot.ExemptFrom);
        Assert.True(lot.IsExemptToday);
        Assert.Equal(1.5m, vusa.Quantity);

        var gis = r.Positions.Single(p => p.Ticker == "GIS");
        Assert.Equal(1m, gis.Quantity);           // bought 2, sold 1
        Assert.Equal(59.35m, gis.CostEur);        // half of 118.70
    }

    [Fact]
    public async Task Tax_estimate_uses_supplied_prices()
    {
        // A lot bought 2026-06-01 is not yet exempt on the fixed "today" 2026-11-18.
        var header = ApiFactory.SampleCsv.Split('\n')[0];
        await ImportAsync(header + "\nMarket buy,2026-06-01 10:00:00,IE00B3XXRP09,VUSA,Vanguard S&P 500 UCITS ETF,,EOF9,10,10.00,EUR,1,,,100.00,EUR,,,,\n");

        var response = await _client.PostAsJsonAsync("/api/positions/tax-estimate", new { pricesEur = new Dictionary<string, decimal> { [Vusa] = 15m } }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var r = (await response.Content.ReadFromJsonAsync<EstimateResponse>(Ct))!;

        Assert.Equal(Disclaimer.Text, r.Disclaimer);
        Assert.Equal(new DateOnly(2026, 11, 18), r.Estimate.AsOf);
        Assert.Equal(50m, r.Estimate.HypotheticalTaxableGainEur);
        Assert.Equal(6.00m, r.Estimate.EstimatedTaxEur);
    }

    [Fact]
    public async Task Tax_estimate_warns_about_positions_without_price()
    {
        await ImportAsync(ApiFactory.SampleCsv);
        var response = await _client.PostAsJsonAsync("/api/positions/tax-estimate", new { pricesEur = new Dictionary<string, decimal> { [Vusa] = 100m } }, Ct);
        var r = (await response.Content.ReadFromJsonAsync<EstimateResponse>(Ct))!;
        Assert.Single(r.Estimate.Positions);
        Assert.Equal(2, r.Estimate.Warnings.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Tax_estimate_rejects_non_positive_price(decimal price)
    {
        var response = await _client.PostAsJsonAsync("/api/positions/tax-estimate", new { pricesEur = new Dictionary<string, decimal> { [Vusa] = price } }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tax_estimate_rejects_missing_prices()
    {
        var response = await _client.PostAsJsonAsync("/api/positions/tax-estimate", new { }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_is_served_with_disclaimer()
    {
        var response = await _client.GetAsync("/openapi/v1.json", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Porezna uprava", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Swagger_ui_is_served()
        => Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html", Ct)).StatusCode);
}
