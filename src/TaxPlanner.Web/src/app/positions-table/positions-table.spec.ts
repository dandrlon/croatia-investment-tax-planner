import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PositionsTable } from './positions-table';
import { vhyl, vusaAllExempt } from '../shared/test-data';

describe('PositionsTable', () => {
  let fixture: ComponentFixture<PositionsTable>;
  const el = () => fixture.nativeElement as HTMLElement;
  const rows = () => Array.from(el().querySelectorAll<HTMLElement>('tr.position-row'));

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PositionsTable] }).compileComponents();
    fixture = TestBed.createComponent(PositionsTable);
    fixture.componentRef.setInput('positions', [vusaAllExempt, vhyl]);
    fixture.detectChanges();
  });

  it('shows "all exempt" or the next date with shares', () => {
    expect(rows()[0].textContent).toContain('VUSA');
    expect(rows()[0].textContent).toContain('all exempt');
    expect(rows()[1].textContent).toContain('VHYL');
    expect(rows()[1].textContent).toContain('3 Mar 2027 (15 sh)');
  });

  it('expands a position to its lots on click', () => {
    expect(el().querySelectorAll('.lot').length).toBe(0);

    rows()[1].click();
    fixture.detectChanges();
    const lots = Array.from(el().querySelectorAll('.lot'));
    expect(lots.length).toBe(4);
    expect(lots[0].textContent).toContain('1 Jan 2025');
    expect(lots[0].textContent).toContain('10 Jan 2026');
    expect(lots[0].textContent).toContain('tax-free');
    expect(lots[1].textContent).not.toContain('tax-free');
  });
});
