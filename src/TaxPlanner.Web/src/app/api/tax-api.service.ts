import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { DividendsResponse, ImportResult, ImportSummary, PositionsResponse } from './models';

/** Calls the Tax Planner API. URLs are relative, so the UI and the API share one origin. */
@Injectable({ providedIn: 'root' })
export class TaxApiService {
  private readonly http = inject(HttpClient);

  /** The last import, or null when nothing has been imported yet (the API answers 204). */
  getLatestImport(): Observable<ImportSummary | null> {
    return this.http
      .get<ImportSummary>('/api/imports/latest', { observe: 'response' })
      .pipe(map((response) => (response.status === 204 ? null : response.body)));
  }

  uploadCsv(file: File): Observable<ImportResult> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ImportResult>('/api/imports', form);
  }

  getDividends(): Observable<DividendsResponse> {
    return this.http.get<DividendsResponse>('/api/dividends');
  }

  getPositions(): Observable<PositionsResponse> {
    return this.http.get<PositionsResponse>('/api/positions');
  }
}
