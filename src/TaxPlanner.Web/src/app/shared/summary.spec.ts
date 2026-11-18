import { collectWarnings, currentYear, nextExemption, nextExemptionOverall, taxDueForYear } from './summary';
import { dividends, positions, realtyIncome, vhyl, vusaAllExempt, year } from './test-data';

describe('taxDueForYear', () => {
  it('is €0 for a year without dividends', () => {
    expect(taxDueForYear([], 2026)).toEqual({ kind: 'amount', eur: 0 });
  });

  it('is unverified when the API returns no tax for the year', () => {
    expect(taxDueForYear([year(2023, { rate: null, taxDueEur: null })], 2023)).toEqual({ kind: 'unverified' });
  });

  it('is the API tax due for the year', () => {
    expect(taxDueForYear([year(2025, { taxDueEur: 21.08 }), year(2026)], 2025)).toEqual({ kind: 'amount', eur: 21.08 });
  });
});

describe('nextExemption', () => {
  it('is the earliest future date with all shares that become exempt on it', () => {
    expect(nextExemption(vhyl)).toEqual({ date: '2027-03-03', quantity: 15 });
  });

  it('is null when every lot is already exempt', () => {
    expect(nextExemption(vusaAllExempt)).toBeNull();
  });
});

describe('nextExemptionOverall', () => {
  it('is the position with the earliest next exemption', () => {
    expect(nextExemptionOverall([vusaAllExempt, realtyIncome, vhyl])).toEqual({ ticker: 'VHYL', date: '2027-03-03', quantity: 15 });
  });

  it('is null when nothing is pending', () => {
    expect(nextExemptionOverall([vusaAllExempt])).toBeNull();
    expect(nextExemptionOverall([])).toBeNull();
  });
});

describe('collectWarnings', () => {
  it('lists year warnings then position warnings without duplicates', () => {
    const divs = dividends([year(2024, { warnings: ['a'] }), year(2025, { warnings: ['b'] })]);
    expect(collectWarnings(divs, positions([], ['b', 'c']))).toEqual(['a', 'b', 'c']);
  });
});

describe('currentYear', () => {
  it('is the year of the given date', () => {
    expect(currentYear(new Date(2026, 10, 18))).toBe(2026);
  });
});
