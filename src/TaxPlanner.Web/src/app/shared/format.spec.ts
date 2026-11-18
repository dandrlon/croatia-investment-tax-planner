import { formatEur, formatIsoDate, formatTimestamp } from './format';

describe('formatEur', () => {
  it('formats euros with two decimals', () => {
    expect(formatEur(14.62)).toBe('€14.62');
    expect(formatEur(0)).toBe('€0.00');
    expect(formatEur(1234.5)).toBe('€1,234.50');
  });
});

describe('formatIsoDate', () => {
  it('formats a date without shifting it by time zone', () => {
    expect(formatIsoDate('2027-03-03')).toBe('3 Mar 2027');
    expect(formatIsoDate('2026-12-31')).toBe('31 Dec 2026');
    expect(formatIsoDate('2026-01-01')).toBe('1 Jan 2026');
  });
});

describe('formatTimestamp', () => {
  it('formats a timestamp in local time as "d MMM yyyy, HH:mm"', () => {
    const local = new Date(2026, 10, 18, 14, 2);
    expect(formatTimestamp(local.toISOString())).toBe('18 Nov 2026, 14:02');
  });
});
