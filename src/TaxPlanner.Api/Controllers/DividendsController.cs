using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxPlanner.Core;
using TaxPlanner.Core.Dividends;
using TaxPlanner.Core.Transactions;
using TaxPlanner.Infrastructure;

namespace TaxPlanner.Api.Controllers;

public record DividendsResponse(string Disclaimer, IReadOnlyList<DividendYearSummary> Years);

[ApiController]
[Route("api/dividends")]
public class DividendsController(TaxPlannerDbContext db) : ControllerBase
{
    /// <summary>Dividends per year and position, with the Croatian tax owed after credit for foreign withholding.</summary>
    /// <param name="year">Only return this calendar year.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<DividendsResponse> Get(int? year, CancellationToken ct)
    {
        var dividends = await db.Transactions.AsNoTracking()
            .Where(t => t.Type == TransactionType.Dividend)
            .ToListAsync(ct);

        var years = DividendTaxCalculator.Calculate(dividends)
            .Where(y => year is null || y.Year == year)
            .ToList();

        return new DividendsResponse(Disclaimer.Text, years);
    }
}
