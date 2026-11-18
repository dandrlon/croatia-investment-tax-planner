using Microsoft.EntityFrameworkCore;
using TaxPlanner.Api.Services;
using TaxPlanner.Core;
using TaxPlanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Croatian Investment Tax Planner API";
    document.Info.Description =
        $"Dividend tax and capital gains exemption dates for Trading 212 exports. **{Disclaimer.Text}**";
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<TaxPlannerDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("TaxPlanner")
        ?? throw new InvalidOperationException("Connection string 'TaxPlanner' is not configured."),
    sql => sql.EnableRetryOnFailure()));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ImportService>();

var app = builder.Build();

// Single-user local app: apply pending migrations on startup so `docker compose up` is enough.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<TaxPlannerDbContext>().Database.MigrateAsync();
}

// Swagger is on in every environment: this is a single-user app that runs locally.
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Tax Planner API"));
app.MapControllers();

app.Run();

/// <summary>Exposed so integration tests can start the app with WebApplicationFactory.</summary>
public partial class Program;
