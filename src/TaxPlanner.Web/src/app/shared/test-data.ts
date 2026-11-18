// Builders for API data used across unit tests.
import { DividendYear, DividendsResponse, Lot, Position, PositionsResponse } from '../api/models';

export function year(year: number, overrides: Partial<DividendYear> = {}): DividendYear {
  return {
    year,
    rate: 0.12,
    grossEur: 100,
    withheldEur: 0,
    croatianTaxEur: 12,
    foreignTaxCreditEur: 0,
    taxDueEur: 12,
    positions: [],
    warnings: [],
    ...overrides,
  };
}

export function lot(exemptFrom: string, quantity: number, isExemptToday = false, acquiredOn = '2025-01-01'): Lot {
  return { acquiredOn, quantity, costEur: quantity * 10, exemptFrom, isExemptToday };
}

export function position(ticker: string, lots: Lot[]): Position {
  return {
    isin: `ISIN-${ticker}`,
    ticker,
    name: `${ticker} name`,
    quantity: lots.reduce((sum, l) => sum + l.quantity, 0),
    costEur: lots.reduce((sum, l) => sum + l.costEur, 0),
    lots,
  };
}

export function dividends(years: DividendYear[]): DividendsResponse {
  return { disclaimer: 'This is not tax advice.', years };
}

export function positions(items: Position[], warnings: string[] = []): PositionsResponse {
  return { disclaimer: 'This is not tax advice.', positions: items, warnings };
}

/** VUSA all exempt, VHYL with 12 + 3 shares on 2027-03-03 and 5 on 2027-06-14, O with 5 on 2027-06-14. */
export const vusaAllExempt = position('VUSA', [lot('2026-03-16', 42.5, true, '2024-03-15')]);
export const vhyl = position('VHYL', [
  lot('2026-01-10', 30, true),
  lot('2027-06-14', 5),
  lot('2027-03-03', 12),
  lot('2027-03-03', 3),
]);
export const realtyIncome = position('O', [lot('2027-06-14', 5)]);
