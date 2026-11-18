import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { By } from '@angular/platform-browser';
import { Dashboard } from './dashboard';
import { ImportBar } from '../import-bar/import-bar';
import { TaxApiService } from '../api/tax-api.service';
import { ImportResult, ImportSummary } from '../api/models';
import { dividends, positions, vhyl, year } from '../shared/test-data';

describe('Dashboard', () => {
  let fixture: ComponentFixture<Dashboard>;
  let latest: Observable<ImportSummary | null>;
  let getLatestImport: ReturnType<typeof vi.fn>;

  const summary: ImportSummary = {
    id: 1, fileName: 'export.csv', importedAtUtc: '2026-11-18T10:00:00Z',
    importedCount: 6, duplicateCount: 0, skippedCount: 2, errorCount: 0,
  };
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  async function render() {
    getLatestImport = vi.fn(() => latest);
    const api = {
      getLatestImport,
      getDividends: () => of(dividends([year(2024, { taxDueEur: 0.04, warnings: ['History may be incomplete.'] })])),
      getPositions: () => of(positions([vhyl])),
      uploadCsv: () => of<ImportResult>({ importId: 2, imported: 1, duplicates: 0, skipped: [], errors: [{ line: 9, message: 'Total is in GBP.' }] }),
    };
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [{ provide: TaxApiService, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
  }

  it('shows the empty state and disclaimer when nothing is imported', async () => {
    latest = of(null);
    await render();
    expect(fixture.componentInstance.state()).toBe('empty');
    expect(text()).toContain('Upload your first Trading 212 export');
    expect(text()).toContain('History → Export history');
    expect(text()).toContain('This is not tax advice.');
    expect(text()).not.toContain('Dividends by year');
  });

  it('shows all sections when data exists', async () => {
    latest = of(summary);
    await render();
    expect(fixture.componentInstance.state()).toBe('ready');
    expect(text()).toContain('Last import: export.csv');
    expect(text()).toContain('⚠ 1 warning');
    expect(text()).toContain('Dividend tax due');
    expect(text()).toContain('Dividends by year');
    expect(text()).toContain('Positions');
    expect(text()).toContain('VHYL');
    expect(text()).toContain('This is not tax advice.');
  });

  it('shows an error with Retry when a request fails, and reloads on Retry', async () => {
    latest = throwError(() => new Error('offline'));
    await render();
    expect(fixture.componentInstance.state()).toBe('error');
    expect(text()).toContain('Could not load your data.');

    latest = of(summary);
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.retry')!.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.state()).toBe('ready');
  });

  it('keeps the upload result and row errors visible after the reload, including the first upload', async () => {
    latest = of(null);
    await render();
    expect(fixture.componentInstance.state()).toBe('empty');

    latest = of(summary);
    const bar = fixture.debugElement.query(By.directive(ImportBar)).componentInstance as ImportBar;
    bar.upload(new File(['x'], 'export.csv'));
    fixture.detectChanges();

    expect(fixture.componentInstance.state()).toBe('ready');
    expect(text()).toContain('1 new, 0 already imported, 0 skipped, 1 errors');
    expect(text()).toContain('Line 9: Total is in GBP.');
  });

  it('reloads all data after an upload', async () => {
    latest = of(summary);
    await render();
    fixture.componentInstance.onUploaded();
    expect(getLatestImport).toHaveBeenCalledTimes(2);
  });
});
