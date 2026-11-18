import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { App } from './app';
import { TaxApiService } from './api/tax-api.service';

describe('App', () => {
  it('renders the dashboard', async () => {
    const api = { getLatestImport: () => of(null), getDividends: () => of({ disclaimer: '', years: [] }), getPositions: () => of({ disclaimer: '', positions: [], warnings: [] }) };
    await TestBed.configureTestingModule({ imports: [App], providers: [{ provide: TaxApiService, useValue: api }] }).compileComponents();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain('Croatian Investment Tax Planner');
  });
});
