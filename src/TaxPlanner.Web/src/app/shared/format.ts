// Formatting for amounts and dates shown in the UI.

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const eur = new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' });

/** 14.62 → "€14.62" */
export function formatEur(value: number): string {
  return eur.format(value);
}

const shares = new Intl.NumberFormat('en-IE', { maximumFractionDigits: 6 });

/**
 * Share counts, which are often fractional: 0.1 + 0.2 → "0.3" rather than "0.30000000000000004".
 * Six decimals is more precision than Trading 212 shows.
 */
export function formatQuantity(value: number): string {
  return shares.format(value);
}

/**
 * "2027-03-03" → "3 Mar 2027".
 * Reads the parts of the string directly instead of building a Date, because
 * `new Date('2027-03-03')` is midnight UTC and would show 2 Mar west of UTC.
 */
export function formatIsoDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return `${day} ${MONTHS[month - 1]} ${year}`;
}

/** An ISO timestamp → "18 Nov 2026, 14:02" in the browser's local time. */
export function formatTimestamp(isoUtc: string): string {
  const d = new Date(isoUtc);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}, ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
