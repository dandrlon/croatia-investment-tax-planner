namespace TaxPlanner.Core.Transactions;

/// <summary>One uploaded CSV file and what happened to its rows.</summary>
public class ImportBatch
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public DateTimeOffset ImportedAtUtc { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int SkippedCount { get; set; }

    /// <summary>Rows that could not be read; each one is a transaction missing from the reports.</summary>
    public int ErrorCount { get; set; }
}
