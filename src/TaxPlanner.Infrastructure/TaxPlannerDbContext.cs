using Microsoft.EntityFrameworkCore;
using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Infrastructure;

public class TaxPlannerDbContext(DbContextOptions<TaxPlannerDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ImportBatch> Imports => Set<ImportBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportBatch>(e =>
        {
            e.Property(b => b.FileName).HasMaxLength(260);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasOne<ImportBatch>().WithMany().HasForeignKey(t => t.ImportBatchId);
            e.HasIndex(t => t.SourceHash).IsUnique();
            e.HasIndex(t => t.Isin);

            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(16);
            e.Property(t => t.Isin).HasMaxLength(12);
            e.Property(t => t.Ticker).HasMaxLength(32);
            e.Property(t => t.Name).HasMaxLength(256);
            e.Property(t => t.PriceCurrency).HasMaxLength(3);
            e.Property(t => t.WithholdingTaxCurrency).HasMaxLength(3);
            e.Property(t => t.SourceHash).HasMaxLength(64).IsFixedLength();

            e.Property(t => t.Quantity).HasPrecision(19, 8);
            e.Property(t => t.ExchangeRate).HasPrecision(19, 8);
            e.Property(t => t.PricePerShare).HasPrecision(18, 4);
            e.Property(t => t.TotalEur).HasPrecision(18, 4);
            e.Property(t => t.WithholdingTax).HasPrecision(18, 4);
            // Converted from another currency, so keep more decimals than a stored amount.
            e.Property(t => t.WithholdingTaxEur).HasPrecision(19, 8);
        });
    }
}
