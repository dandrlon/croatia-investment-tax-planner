using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxPlanner.Infrastructure;

namespace TaxPlanner.Api.Tests;

[Collection(ApiCollection.Name)]
public class ImportsApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private record Skipped(int Line, string Action, string Reason);
    private record Error(int Line, string Message);
    private record Result(int ImportId, int Imported, int Duplicates, List<Skipped> Skipped, List<Error> Errors);

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string[] SampleLines => ApiFactory.SampleCsv.Split('\n');

    private async Task<Result> ImportAsync(string csv)
    {
        var response = await _client.PostAsync("/api/imports", ApiFactory.CsvUpload(csv), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Result>(Ct))!;
    }

    private async Task<int> StoredTransactionCountAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<TaxPlannerDbContext>().Transactions.CountAsync(Ct);
    }

    [Fact]
    public async Task Import_sample_returns_counts()
    {
        var r = await ImportAsync(ApiFactory.SampleCsv);
        Assert.True(r.ImportId > 0);
        Assert.Equal(6, r.Imported);
        Assert.Equal(0, r.Duplicates);
        Assert.Equal(2, r.Skipped.Count);
        Assert.Empty(r.Errors);
        Assert.Equal(6, await StoredTransactionCountAsync());
    }

    [Fact]
    public async Task Reimporting_same_file_imports_nothing_new()
    {
        await ImportAsync(ApiFactory.SampleCsv);
        var second = await ImportAsync(ApiFactory.SampleCsv);
        Assert.Equal(0, second.Imported);
        Assert.Equal(6, second.Duplicates);
        Assert.Equal(6, await StoredTransactionCountAsync());
    }

    [Fact]
    public async Task Overlapping_export_imports_only_new_rows()
    {
        var l = SampleLines;
        // header + the first four buy/sell/dividend rows (file lines 3–6)
        Assert.Equal(4, (await ImportAsync(string.Join('\n', l[0], l[2], l[3], l[4], l[5]))).Imported);

        var full = await ImportAsync(ApiFactory.SampleCsv);
        Assert.Equal(2, full.Imported);
        Assert.Equal(4, full.Duplicates);
    }

    [Fact]
    public async Task Duplicate_rows_within_one_file_are_stored_once()
    {
        var l = SampleLines;
        var r = await ImportAsync(string.Join('\n', l[0], l[2], l[2]));
        Assert.Equal(1, r.Imported);
        Assert.Equal(1, r.Duplicates);
    }

    [Fact]
    public async Task Row_errors_are_reported_and_valid_rows_still_imported()
    {
        var l = SampleLines;
        var broken = l[2].Replace("129.18,EUR", "129.18,GBP");
        var r = await ImportAsync(string.Join('\n', l[0], broken, l[3]));
        Assert.Equal(1, r.Imported);
        Assert.Equal(2, Assert.Single(r.Errors).Line);
    }

    [Fact]
    public async Task Missing_file_returns_400()
    {
        var response = await _client.PostAsync("/api/imports", new MultipartFormDataContent { { new StringContent("x"), "other" } }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_file_returns_400()
    {
        var response = await _client.PostAsync("/api/imports", ApiFactory.CsvUpload(""), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oversized_file_returns_400()
    {
        var huge = new string('x', 10 * 1024 * 1024 + 1);
        var response = await _client.PostAsync("/api/imports", ApiFactory.CsvUpload(huge), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task File_missing_required_column_returns_400_problem_details()
    {
        var response = await _client.PostAsync("/api/imports", ApiFactory.CsvUpload("Action,Time\nMarket buy,2025-01-10 10:00:00\n"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Contains("Missing required column", problem!.Detail);
        Assert.Equal(0, await StoredTransactionCountAsync());
    }
}
