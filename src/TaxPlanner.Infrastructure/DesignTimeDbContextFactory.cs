using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaxPlanner.Infrastructure;

/// <summary>
/// Used only by the `dotnet ef` tools to create migrations. It never connects to a database,
/// so the connection string is a placeholder.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaxPlannerDbContext>
{
    public TaxPlannerDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<TaxPlannerDbContext>()
            .UseSqlServer("Server=localhost;Database=TaxPlanner;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}
