import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TaxApiService } from './tax-api.service';
import { ImportSummary } from './models';

describe('TaxApiService', () => {
  let api: TaxApiService;
  let http: HttpTestingController;

  const summary: ImportSummary = {
    id: 2,
    fileName: 'export.csv',
    importedAtUtc: '2026-11-18T10:00:00+00:00',
    importedCount: 37,
    duplicateCount: 375,
    skippedCount: 2,
    errorCount: 0,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(TaxApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('maps 204 from /api/imports/latest to null', () => {
    let result: ImportSummary | null | undefined;
    api.getLatestImport().subscribe((r) => (result = r));
    http.expectOne('/api/imports/latest').flush(null, { status: 204, statusText: 'No Content' });
    expect(result).toBeNull();
  });

  it('returns the latest import summary', () => {
    let result: ImportSummary | null | undefined;
    api.getLatestImport().subscribe((r) => (result = r));
    http.expectOne('/api/imports/latest').flush(summary);
    expect(result).toEqual(summary);
  });

  it('posts the file as multipart field "file"', () => {
    const file = new File(['Action,Time\n'], 'export.csv', { type: 'text/csv' });
    api.uploadCsv(file).subscribe();
    const req = http.expectOne('/api/imports');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    expect((req.request.body as FormData).get('file')).toBe(file);
    req.flush({ importId: 1, imported: 0, duplicates: 0, skipped: [], errors: [] });
  });

  it('gets dividends and positions from their endpoints', () => {
    api.getDividends().subscribe();
    api.getPositions().subscribe();
    http.expectOne({ method: 'GET', url: '/api/dividends' }).flush({ disclaimer: 'd', years: [] });
    http.expectOne({ method: 'GET', url: '/api/positions' }).flush({ disclaimer: 'd', positions: [], warnings: [] });
  });
});
