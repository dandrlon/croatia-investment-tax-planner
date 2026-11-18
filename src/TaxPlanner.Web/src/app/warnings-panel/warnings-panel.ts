import { Component, computed, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

/** All report warnings behind one collapsed "⚠ N warnings" toggle. Renders nothing when there are none. */
@Component({
  selector: 'app-warnings-panel',
  imports: [MatButtonModule],
  template: `
    @if (warnings().length > 0) {
      <section class="warnings">
        <button mat-button type="button" (click)="open.set(!open())" [attr.aria-expanded]="open()">{{ label() }}</button>
        @if (open()) {
          <ul>
            @for (w of warnings(); track w) {
              <li>{{ w }}</li>
            }
          </ul>
        }
      </section>
    }
  `,
  styles: `
    .warnings {
      border: 1px solid var(--mat-sys-error);
      border-radius: 12px;
      padding: 4px 8px;
    }
    button {
      color: var(--mat-sys-error);
    }
    ul {
      margin: 0 0 8px;
      padding-left: 28px;
    }
  `,
})
export class WarningsPanel {
  readonly warnings = input.required<string[]>();
  readonly open = signal(false);

  readonly label = computed(() => {
    const n = this.warnings().length;
    return `⚠ ${n} ${n === 1 ? 'warning' : 'warnings'}`;
  });
}
