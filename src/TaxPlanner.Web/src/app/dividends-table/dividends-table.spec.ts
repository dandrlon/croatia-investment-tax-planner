import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DividendsTable } from './dividends-table';
import { DividendYear } from '../api/models';
import { year } from '../shared/test-data';

describe('DividendsTable', () => {
  let fixture: ComponentFixture<DividendsTable>;
  const el = () => fixture.nativeElement as HTMLElement;
  const yearRows = () => Array.from(el().querySelectorAll<HTMLElement>('tr.year-row'));

  const vusa = { isin: 'IE00B3XXRP09', ticker: 'VUSA', name: 'Vanguard S&P 500', grossEur: 50, withheldEur: 0, croatianTaxEur: 6, foreignTaxCreditEur: 0, taxDueEur: 6 };

  async function render(years: DividendYear[]) {
    await TestBed.configureTestingModule({ imports: [DividendsTable] }).compileComponents();
    fixture = TestBed.createComponent(DividendsTable);
    fixture.componentRef.setInput('years', years);
    fixture.detectChanges();
  }

  it('lists years newest first with formatted amounts', async () => {
    await render([year(2024, { grossEur: 88 }), year(2025, { grossEur: 201.1, withheldEur: 3.05, croatianTaxEur: 24.13, taxDueEur: 21.08 })]);
    expect(yearRows().map((r) => r.querySelector('td')!.textContent!.trim())).toEqual(['2025', '2024']);
    expect(yearRows()[0].textContent).toContain('€201.10');
    expect(yearRows()[0].textContent).toContain('€3.05');
    expect(yearRows()[0].textContent).toContain('€24.13');
    expect(yearRows()[0].textContent).toContain('€21.08');
  });

  it('shows — and a warning mark for a year without a verified rate', async () => {
    await render([year(2023, { rate: null, croatianTaxEur: null, foreignTaxCreditEur: null, taxDueEur: null, warnings: ['No verified tax rate.'] })]);
    expect(yearRows()[0].textContent).toContain('—');
    expect(yearRows()[0].textContent).toContain('⚠');
  });

  it('expands a year to its positions on click', async () => {
    await render([year(2025, { positions: [vusa] })]);
    expect(el().textContent).not.toContain('VUSA');

    yearRows()[0].click();
    fixture.detectChanges();
    expect(el().textContent).toContain('VUSA');
    expect(el().textContent).toContain('Vanguard S&P 500');

    yearRows()[0].click();
    fixture.detectChanges();
    expect(el().textContent).not.toContain('VUSA');
  });
});
