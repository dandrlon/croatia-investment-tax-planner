import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TaxApiService } from '../api/tax-api.service';
import { DividendsResponse, ImportSummary, PositionsResponse } from '../api/models';
import { collectWarnings, currentYear } from '../shared/summary';
import { ImportBar } from '../import-bar/import-bar';
import { WarningsPanel } from '../warnings-panel/warnings-panel';
import { SummaryTiles } from '../summary-tiles/summary-tiles';
import { DividendsTable } from '../dividends-table/dividends-table';
import { PositionsTable } from '../positions-table/positions-table';

type PageState = 'loading' | 'error' | 'empty' | 'ready';

/** The whole page: loads the last import and both reports, and reloads them after an upload. */
@Component({
  selector: 'app-dashboard',
  imports: [MatButtonModule, MatProgressBarModule, ImportBar, WarningsPanel, SummaryTiles, DividendsTable, PositionsTable],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly api = inject(TaxApiService);

  readonly state = signal<PageState>('loading');
  readonly latest = signal<ImportSummary | null>(null);
  readonly dividends = signal<DividendsResponse | null>(null);
  readonly positions = signal<PositionsResponse | null>(null);
  readonly warnings = signal<string[]>([]);
  readonly year = currentYear();

  constructor() {
    this.load();
  }

  /**
   * Loads the last import and both reports. The page shows the spinner only when it has nothing
   * to show yet; a reload after an upload keeps the current page (and the upload result) on screen.
   */
  load(): void {
    if (this.state() === 'error') this.state.set('loading');
    forkJoin({
      latest: this.api.getLatestImport(),
      dividends: this.api.getDividends(),
      positions: this.api.getPositions(),
    }).subscribe({
      next: ({ latest, dividends, positions }) => {
        this.latest.set(latest);
        this.dividends.set(dividends);
        this.positions.set(positions);
        this.warnings.set(collectWarnings(dividends, positions));
        this.state.set(latest === null ? 'empty' : 'ready');
      },
      error: () => this.state.set('error'),
    });
  }

  onUploaded(): void {
    this.load();
  }
}
