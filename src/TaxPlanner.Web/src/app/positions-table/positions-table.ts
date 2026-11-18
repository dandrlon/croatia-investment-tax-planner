import { Component, computed, input, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { Position } from '../api/models';
import { formatEur, formatIsoDate, formatQuantity } from '../shared/format';
import { nextExemption } from '../shared/summary';

/** Open positions with their next tax-free date. Clicking a position shows its lots. */
@Component({
  selector: 'app-positions-table',
  imports: [MatTableModule, MatChipsModule],
  templateUrl: './positions-table.html',
  styleUrl: './positions-table.scss',
})
export class PositionsTable {
  readonly positions = input.required<Position[]>();

  readonly columns = ['ticker', 'name', 'shares', 'nextTaxFree'];
  readonly expanded = signal<ReadonlySet<string>>(new Set());

  /** Positions paired with the text for their "Next tax-free date" column. */
  readonly rows = computed(() =>
    this.positions().map((p) => {
      const next = nextExemption(p);
      return {
        position: p,
        nextTaxFree: next ? `${formatIsoDate(next.date)} (${formatQuantity(next.quantity)} sh)` : 'all exempt',
      };
    }),
  );

  toggle(isin: string): void {
    const next = new Set(this.expanded());
    if (next.has(isin)) next.delete(isin);
    else next.add(isin);
    this.expanded.set(next);
  }

  protected readonly formatEur = formatEur;
  protected readonly formatIsoDate = formatIsoDate;
  protected readonly formatQuantity = formatQuantity;
}
