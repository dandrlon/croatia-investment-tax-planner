import { TestBed } from '@angular/core/testing';
import { SummaryTiles } from './summary-tiles';
import { DividendYear, Position } from '../api/models';
import { vhyl, vusaAllExempt, year } from '../shared/test-data';

describe('SummaryTiles', () => {
  async function render(years: DividendYear[], positions: Position[]): Promise<string> {
    await TestBed.configureTestingModule({ imports: [SummaryTiles] }).compileComponents();
    const fixture = TestBed.createComponent(SummaryTiles);
    fixture.componentRef.setInput('years', years);
    fixture.componentRef.setInput('positions', positions);
    fixture.componentRef.setInput('year', 2026);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('shows tax due for this and last year', async () => {
    const text = await render([year(2026, { taxDueEur: 14.62 }), year(2025, { taxDueEur: 21.08 })], []);
    expect(text).toContain('Dividend tax due 2026');
    expect(text).toContain('€14.62');
    expect(text).toContain('Dividend tax due 2025');
    expect(text).toContain('€21.08');
  });

  it('shows €0.00 for a year without dividends', async () => {
    const text = await render([year(2026, { taxDueEur: 14.62 })], []);
    expect(text).toContain('€0.00');
  });

  it('shows — and "No verified tax rate" for an unverified year', async () => {
    const text = await render([year(2025, { rate: null, taxDueEur: null })], []);
    expect(text).toContain('—');
    expect(text).toContain('No verified tax rate');
  });

  it('shows the next lot to become tax-free', async () => {
    const text = await render([], [vusaAllExempt, vhyl]);
    expect(text).toContain('Next lot becomes tax-free');
    expect(text).toContain('VHYL · 3 Mar 2027');
  });

  it('says "All lots are tax-free" when none is pending', async () => {
    const text = await render([], [vusaAllExempt]);
    expect(text).toContain('All lots are tax-free');
  });
});
