import { Component, computed, input, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { DividendYear } from '../api/models';
import { formatEur } from '../shared/format';

/** Dividends per year, newest first. Clicking a year shows its positions. */
@Component({
  selector: 'app-dividends-table',
  imports: [MatTableModule],
  templateUrl: './dividends-table.html',
  styleUrl: './dividends-table.scss',
})
export class DividendsTable {
  readonly years = input.required<DividendYear[]>();

  readonly columns = ['year', 'gross', 'withheld', 'croatianTax', 'taxDue'];
  readonly sorted = computed(() => [...this.years()].sort((a, b) => b.year - a.year));
  readonly expanded = signal<ReadonlySet<number>>(new Set());

  toggle(year: number): void {
    const next = new Set(this.expanded());
    if (next.has(year)) next.delete(year);
    else next.add(year);
    this.expanded.set(next);
  }

  /** An amount, or "—" when the API has no value (no verified tax rate for that year). */
  money(value: number | null): string {
    return value === null ? '—' : formatEur(value);
  }
}
