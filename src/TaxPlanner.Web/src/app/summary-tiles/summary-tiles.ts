import { Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { DividendYear, Position } from '../api/models';
import { formatEur, formatIsoDate } from '../shared/format';
import { TileValue, nextExemptionOverall, taxDueForYear } from '../shared/summary';

/** Three headline numbers: dividend tax due this year and last year, and the next lot to become tax-free. */
@Component({
  selector: 'app-summary-tiles',
  imports: [MatCardModule],
  template: `
    <div class="tiles">
      @for (tile of taxTiles(); track tile.year) {
        <mat-card appearance="outlined">
          <mat-card-content>
            <div class="label">Dividend tax due {{ tile.year }}</div>
            @if (tile.value.kind === 'amount') {
              <div class="value">{{ formatEur(tile.value.eur) }}</div>
            } @else {
              <div class="value">—</div>
              <div class="sub">No verified tax rate</div>
            }
          </mat-card-content>
        </mat-card>
      }
      <mat-card appearance="outlined">
        <mat-card-content>
          <div class="label">Next lot becomes tax-free</div>
          @if (next(); as n) {
            <div class="value">{{ n.ticker }} · {{ formatIsoDate(n.date) }}</div>
          } @else {
            <div class="value">All lots are tax-free</div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: `
    .tiles {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 12px;
    }
    .label,
    .sub {
      font-size: 0.875rem;
      color: var(--mat-sys-on-surface-variant);
    }
    .value {
      font-size: 1.5rem;
      font-weight: 500;
      margin-top: 4px;
    }
  `,
})
export class SummaryTiles {
  readonly years = input.required<DividendYear[]>();
  readonly positions = input.required<Position[]>();
  /** The current calendar year; the tiles show it and the year before. */
  readonly year = input.required<number>();

  readonly taxTiles = computed<{ year: number; value: TileValue }[]>(() =>
    [this.year(), this.year() - 1].map((y) => ({ year: y, value: taxDueForYear(this.years(), y) })),
  );
  readonly next = computed(() => nextExemptionOverall(this.positions()));

  protected readonly formatEur = formatEur;
  protected readonly formatIsoDate = formatIsoDate;
}
