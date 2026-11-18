using TaxPlanner.Core.Rules;
using TaxPlanner.Core.Transactions;

namespace TaxPlanner.Core.Dividends;

public record DividendYearSummary(
    int Year,
    decimal? Rate,
    decimal GrossEur,
    decimal WithheldEur,
    decimal? CroatianTaxEur,
    decimal? ForeignTaxCreditEur,
    decimal? TaxDueEur,
    IReadOnlyList<DividendPositionSummary> Positions,
    IReadOnlyList<string> Warnings);

public record DividendPositionSummary(
    string Isin,
    string Ticker,
    string Name,
    decimal GrossEur,
    decimal WithheldEur,
    decimal? CroatianTaxEur,
    decimal? ForeignTaxCreditEur,
    decimal? TaxDueEur);

/// <summary>
/// Dividend tax per year and position. For each payment:
/// gross = net + withheld; Croatian tax = gross × rate;
/// credit for foreign withholding = min(withheld, Croatian tax); due = tax − credit.
/// Values are summed unrounded and rounded once at the end.
/// </summary>
public static class DividendTaxCalculator
{
    public static IReadOnlyList<DividendYearSummary> Calculate(IEnumerable<Transaction> transactions)
    {
        var payments = transactions
            .Where(t => t.Type == TransactionType.Dividend)
            .Select(t => new Payment(t, CroatianCalendar.ToLocalDate(t.TimeUtc).Year));

        return payments
            .GroupBy(p => p.Year)
            .OrderBy(g => g.Key)
            .Select(SummariseYear)
            .ToList();
    }

    private static DividendYearSummary SummariseYear(IGrouping<int, Payment> year)
    {
        var rate = TaxRates.CapitalIncomeRate(year.Key);
        var positions = year
            .GroupBy(p => p.Transaction.Isin)
            .Select(g => SummarisePosition(g.ToList(), rate))
            .OrderBy(p => p.Ticker)
            .ToList();

        var totals = Totals.Of(year, rate);
        var warnings = rate is null
            ? new[] { $"No verified tax rate is configured for {year.Key}. Tax for this year is not calculated." }
            : [];

        return new DividendYearSummary(year.Key, rate, Money.Round(totals.Gross), Money.Round(totals.Withheld),
            Money.Round(totals.Tax), Money.Round(totals.Credit), Money.Round(totals.Due), positions, warnings);
    }

    private static DividendPositionSummary SummarisePosition(List<Payment> payments, decimal? rate)
    {
        var first = payments[0].Transaction;
        var totals = Totals.Of(payments, rate);
        return new DividendPositionSummary(first.Isin, first.Ticker, first.Name, Money.Round(totals.Gross),
            Money.Round(totals.Withheld), Money.Round(totals.Tax), Money.Round(totals.Credit), Money.Round(totals.Due));
    }

    private sealed record Payment(Transaction Transaction, int Year)
    {
        public decimal Gross => Transaction.TotalEur + Transaction.WithholdingTaxEur;
    }

    private sealed record Totals(decimal Gross, decimal Withheld, decimal? Tax, decimal? Credit, decimal? Due)
    {
        public static Totals Of(IEnumerable<Payment> payments, decimal? rate)
        {
            decimal gross = 0, withheld = 0, tax = 0, credit = 0;
            foreach (var p in payments)
            {
                gross += p.Gross;
                withheld += p.Transaction.WithholdingTaxEur;
                if (rate is null) continue;
                var paymentTax = p.Gross * rate.Value;
                tax += paymentTax;
                credit += Math.Min(p.Transaction.WithholdingTaxEur, paymentTax);
            }

            return rate is null
                ? new Totals(gross, withheld, null, null, null)
                : new Totals(gross, withheld, tax, credit, tax - credit);
        }
    }
}
