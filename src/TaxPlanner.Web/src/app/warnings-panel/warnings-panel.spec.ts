import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WarningsPanel } from './warnings-panel';

describe('WarningsPanel', () => {
  let fixture: ComponentFixture<WarningsPanel>;
  const el = () => fixture.nativeElement as HTMLElement;

  async function render(warnings: string[]) {
    await TestBed.configureTestingModule({ imports: [WarningsPanel] }).compileComponents();
    fixture = TestBed.createComponent(WarningsPanel);
    fixture.componentRef.setInput('warnings', warnings);
    fixture.detectChanges();
  }

  it('is hidden with no warnings', async () => {
    await render([]);
    expect(el().textContent?.trim()).toBe('');
  });

  it('shows "⚠ 2 warnings" collapsed and lists them when clicked', async () => {
    await render(['History may be incomplete.', 'No verified tax rate for 2023.']);
    expect(el().textContent).toContain('⚠ 2 warnings');
    expect(el().querySelectorAll('li').length).toBe(0);

    el().querySelector('button')!.click();
    fixture.detectChanges();
    expect(el().querySelectorAll('li').length).toBe(2);
    expect(el().textContent).toContain('History may be incomplete.');
  });

  it('uses the singular for one warning', async () => {
    await render(['One.']);
    expect(el().textContent).toContain('⚠ 1 warning');
    expect(el().textContent).not.toContain('1 warnings');
  });
});
