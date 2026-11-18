// Picks values out of API responses for the summary tiles and tables.
// Nothing here calculates tax: every amount comes from the API as it is.
import { DividendYear, DividendsResponse, Position, PositionsResponse } from '../api/models';

export type TileValue = { kind: 'amount'; eur: number } | { kind: 'unverified' };

export interface Exemption {
  date: string;
  quantity: number;
}

/** Tax due for one year: €0 when there were no dividends, "unverified" when the API has no rate for it. */
export function taxDueForYear(years: DividendYear[], year: number): TileValue {
  const found = years.find((y) => y.year === year);
  if (!found) return { kind: 'amount', eur: 0 };
  return found.taxDueEur === null ? { kind: 'unverified' } : { kind: 'amount', eur: found.taxDueEur };
}

/** The earliest date on which more of this position becomes tax-free, and how many shares. */
export function nextExemption(position: Position): Exemption | null {
  const pending = position.lots.filter((l) => !l.isExemptToday);
  if (pending.length === 0) return null;

  // ISO dates (yyyy-MM-dd) sort correctly as strings.
  const date = pending.map((l) => l.exemptFrom).sort()[0];
  const quantity = pending.filter((l) => l.exemptFrom === date).reduce((sum, l) => sum + l.quantity, 0);
  return { date, quantity };
}

/** Across all positions: the next lot to become tax-free. */
export function nextExemptionOverall(positions: Position[]): (Exemption & { ticker: string }) | null {
  let best: (Exemption & { ticker: string }) | null = null;
  for (const position of positions) {
    const next = nextExemption(position);
    if (next && (best === null || next.date < best.date)) best = { ticker: position.ticker, ...next };
  }
  return best;
}

/** All warnings from both reports, in order, each once. */
export function collectWarnings(dividends: DividendsResponse, positions: PositionsResponse): string[] {
  const all = [...dividends.years.flatMap((y) => y.warnings), ...positions.warnings];
  return [...new Set(all)];
}

export function currentYear(now: Date = new Date()): number {
  return now.getFullYear();
}
