using Microsoft.EntityFrameworkCore;
using TaxPlanner.Core.Import;
using TaxPlanner.Core.Transactions;
using TaxPlanner.Infrastructure;

namespace TaxPlanner.Api.Services;

public record ImportResult(int ImportId, int Imported, int Duplicates, IReadOnlyList<SkippedRow> Skipped, IReadOnlyList<ParseError> Errors);

/// <summary>Either the import result, or the reason the whole file was rejected.</summary>
public record ImportOutcome(ImportResult? Result, string? RejectionReason);

/// <summary>
/// Parses an uploaded export and stores its new rows. Rows already stored (or repeated
/// within the same file) are counted as duplicates, so overlapping exports can be imported safely.
/// </summary>
public class ImportService(TaxPlannerDbContext db, TimeProvider time)
{
    public async Task<ImportOutcome> ImportAsync(Stream csv, string fileName, CancellationToken ct)
    {
        var parsed = Trading212CsvParser.Parse(csv);
        var fileError = parsed.Errors.FirstOrDefault(e => e.Line == 1);
        if (fileError is not null)
            return new ImportOutcome(null, fileError.Message);

        var hashes = parsed.Transactions.Select(t => t.SourceHash).Distinct().ToList();
        var seen = (await db.Transactions
                .Where(t => hashes.Contains(t.SourceHash))
                .Select(t => t.SourceHash)
                .ToListAsync(ct))
            .ToHashSet();

        var newRows = new List<Transaction>();
        foreach (var transaction in parsed.Transactions)
        {
            if (seen.Add(transaction.SourceHash))
                newRows.Add(transaction);
        }

        var batch = new ImportBatch
        {
            FileName = Path.GetFileName(fileName),
            ImportedAtUtc = time.GetUtcNow(),
            ImportedCount = newRows.Count,
            DuplicateCount = parsed.Transactions.Count - newRows.Count,
            SkippedCount = parsed.Skipped.Count,
            ErrorCount = parsed.Errors.Count,
        };

        // The batch and its rows are stored together or not at all. With retry-on-failure enabled,
        // EF Core requires an explicit transaction to run inside its execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);
            db.Imports.Add(batch);
            await db.SaveChangesAsync(ct);
            newRows.ForEach(t => t.ImportBatchId = batch.Id);
            db.Transactions.AddRange(newRows);
            await db.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct);
        });

        return new ImportOutcome(
            new ImportResult(batch.Id, batch.ImportedCount, batch.DuplicateCount, parsed.Skipped, parsed.Errors),
            null);
    }
}
