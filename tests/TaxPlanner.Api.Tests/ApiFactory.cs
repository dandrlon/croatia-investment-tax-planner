using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TaxPlanner.Infrastructure;
using Testcontainers.MsSql;

namespace TaxPlanner.Api.Tests;

/// <summary>
/// Runs the API in memory against a real SQL Server started in Docker for the test run.
/// "Today" is fixed at 2026-11-18 so date-based results do not change over time.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _sql.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:TaxPlanner", _sql.GetConnectionString());
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now)));
    }

    /// <summary>Deletes all imported data so each test starts from an empty database.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaxPlannerDbContext>();
        await db.Transactions.ExecuteDeleteAsync();
        await db.Imports.ExecuteDeleteAsync();
    }

    public static string FixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-export.csv");

    public static string SampleCsv => File.ReadAllText(FixturePath);

    public static MultipartFormDataContent CsvUpload(string csv, string fileName = "export.csv")
        => new() { { new StringContent(csv), "file", fileName } };
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
