import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { ImportBar } from './import-bar';
import { TaxApiService } from '../api/tax-api.service';
import { ImportResult, ImportSummary } from '../api/models';

describe('ImportBar', () => {
  let fixture: ComponentFixture<ImportBar>;
  let bar: ImportBar;
  let response: Subject<ImportResult>;
  let uploadCsv: ReturnType<typeof vi.fn>;

  const summary: ImportSummary = {
    id: 2,
    fileName: 'trading212-2026-11.csv',
    importedAtUtc: new Date(2026, 10, 18, 14, 2).toISOString(),
    importedCount: 37,
    duplicateCount: 375,
    skippedCount: 2,
    errorCount: 0,
  };

  const csv = (size = 10) => new File(['x'.repeat(size)], 'export.csv', { type: 'text/csv' });
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const button = () => (fixture.nativeElement as HTMLElement).querySelector('button') as HTMLButtonElement;

  beforeEach(async () => {
    response = new Subject<ImportResult>();
    uploadCsv = vi.fn(() => response);
    await TestBed.configureTestingModule({
      imports: [ImportBar],
      providers: [{ provide: TaxApiService, useValue: { uploadCsv } }],
    }).compileComponents();
    fixture = TestBed.createComponent(ImportBar);
    bar = fixture.componentInstance;
    fixture.componentRef.setInput('summary', summary);
    fixture.detectChanges();
  });

  it('shows the last import file name, time and counts', () => {
    expect(text()).toContain('trading212-2026-11.csv');
    expect(text()).toContain('18 Nov 2026, 14:02');
    expect(text()).toContain('37 new · 375 already imported · 2 skipped · 0 errors');
  });

  it('highlights a non-zero error count', () => {
    expect(fixture.nativeElement.querySelector('.warn')).toBeNull();
    fixture.componentRef.setInput('summary', { ...summary, errorCount: 1 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.warn')?.textContent).toContain('1 errors');
  });

  it('rejects a file over 10 MB without calling the API', () => {
    bar.upload(csv(10 * 1024 * 1024 + 1));
    fixture.detectChanges();
    expect(uploadCsv).not.toHaveBeenCalled();
    expect(text()).toContain('The file must be 10 MB or smaller.');
  });

  it('rejects an empty file without calling the API', () => {
    bar.upload(csv(0));
    fixture.detectChanges();
    expect(uploadCsv).not.toHaveBeenCalled();
    expect(text()).toContain('The file is empty.');
  });

  it('disables the button while uploading', () => {
    bar.upload(csv());
    fixture.detectChanges();
    expect(button().disabled).toBe(true);
    bar.upload(csv());
    expect(uploadCsv).toHaveBeenCalledTimes(1);
  });

  it('shows the upload summary and row errors, then emits uploaded', () => {
    const emitted: ImportResult[] = [];
    bar.uploaded.subscribe((r) => emitted.push(r));
    const result: ImportResult = {
      importId: 3,
      imported: 37,
      duplicates: 375,
      skipped: [{ line: 2, action: 'Deposit', reason: 'Not a trade or dividend' }],
      errors: [{ line: 9, message: 'Total is in GBP.' }],
    };

    bar.upload(csv());
    response.next(result);
    response.complete();
    fixture.detectChanges();

    expect(text()).toContain('37 new, 375 already imported, 1 skipped, 1 errors');
    expect(text()).toContain('Line 9: Total is in GBP.');
    expect(button().disabled).toBe(false);
    expect(emitted).toEqual([result]);
  });

  it('shows a re-upload of the same file as 0 new', () => {
    bar.upload(csv());
    response.next({ importId: 4, imported: 0, duplicates: 6, skipped: [], errors: [] });
    fixture.detectChanges();
    expect(text()).toContain('0 new, 6 already imported');
  });

  it('shows the server message for a 400', () => {
    bar.upload(csv());
    response.error(new HttpErrorResponse({ status: 400, error: { title: 'The file could not be imported', detail: "Missing required column 'Total'" } }));
    fixture.detectChanges();
    expect(text()).toContain("Missing required column 'Total'");
    expect(button().disabled).toBe(false);
  });

  it('shows a retry message when the server is unreachable', () => {
    bar.upload(csv());
    response.error(new HttpErrorResponse({ status: 0 }));
    fixture.detectChanges();
    expect(text()).toContain('Could not reach the server. Try again.');
  });

  it('shows the status when the server fails', () => {
    bar.upload(csv());
    response.error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();
    expect(text()).toContain('The upload failed (HTTP 500). Try again.');
    expect(button().disabled).toBe(false);
  });
});
