using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxPlanner.Core;
using TaxPlanner.Core.Lots;
using TaxPlanner.Core.Rules;
using TaxPlanner.Core.Transactions;
using TaxPlanner.Infrastructure;

namespace TaxPlanner.Api.Controllers;

public record LotDto(DateOnly AcquiredOn, decimal Quantity, decimal CostEur, DateOnly ExemptFrom, bool IsExemptToday);

public record PositionDto(string Isin, string Ticker, string Name, decimal Quantity, decimal CostEur, IReadOnlyList<LotDto> Lots);

public record PositionsResponse(string Disclaimer, IReadOnlyList<PositionDto> Positions, IReadOnlyList<string> Warnings);

public class TaxEstimateRequest
{
    /// <summary>Current price per share in EUR, keyed by ISIN.</summary>
    [Required]
    public Dictionary<string, decimal>? PricesEur { get; set; }
}

public record TaxEstimateResponse(string Disclaimer, SaleEstimate Estimate);

[ApiController]
[Route("api/positions")]
public class PositionsController(TaxPlannerDbContext db, TimeProvider time) : ControllerBase
{
    /// <summary>Open positions and their purchase lots (FIFO), with the date each lot becomes exempt from capital gains tax.</summary>
    [HttpGet]
    public async Task<PositionsResponse> Get(CancellationToken ct)
    {
        var lots = await MatchLotsAsync(ct);
        var today = Today();

        var positions = lots.OpenLots
            .GroupBy(l => l.Isin)
            .Select(g => new PositionDto(
                g.Key,
                g.First().Ticker,
                g.First().Name,
                g.Sum(l => l.Quantity),
                Money.Round(g.Sum(l => l.CostEur)),
                g.Select(l => new LotDto(l.AcquiredOn, l.Quantity, Money.Round(l.CostEur), l.ExemptFrom,
                    HoldingPeriod.IsExempt(l.AcquiredOn, today))).ToList()))
            .OrderBy(p => p.Ticker)
            .ToList();

        return new PositionsResponse(Disclaimer.Text, positions, lots.Warnings);
    }

    /// <summary>Estimated capital gains tax for this year if all positions were sold today at the given prices.</summary>
    /// <remarks>Includes gains and losses from sales already made this year. Positions without a price are left out.</remarks>
    [HttpPost("tax-estimate")]
    [ProducesResponseType<TaxEstimateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaxEstimateResponse>> Estimate(TaxEstimateRequest request, CancellationToken ct)
    {
        foreach (var (isin, price) in request.PricesEur!)
        {
            if (price <= 0)
                ModelState.AddModelError($"pricesEur.{isin}", "Price must be greater than zero.");
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var lots = await MatchLotsAsync(ct);
        var estimate = SaleEstimator.Estimate(lots, request.PricesEur, Today());
        return new TaxEstimateResponse(Disclaimer.Text, estimate);
    }

    private async Task<LotMatchResult> MatchLotsAsync(CancellationToken ct)
    {
        var trades = await db.Transactions.AsNoTracking()
            .Where(t => t.Type != TransactionType.Dividend)
            .ToListAsync(ct);
        return FifoLotMatcher.Match(trades);
    }

    private DateOnly Today() => CroatianCalendar.ToLocalDate(time.GetUtcNow());
}
