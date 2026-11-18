using Microsoft.EntityFrameworkCore;
using TaxPlanner.Api.Services;
using TaxPlanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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

app.MapOpenApi();
app.MapControllers();

app.Run();

/// <summary>Exposed so integration tests can start the app with WebApplicationFactory.</summary>
public partial class Program;
