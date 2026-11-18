import { Component, inject, input, output, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TaxApiService } from '../api/tax-api.service';
import { ImportResult, ImportSummary } from '../api/models';
import { formatTimestamp } from '../shared/format';

const MAX_FILE_BYTES = 10 * 1024 * 1024;

/** Shows the last import and lets the user upload a new Trading 212 CSV export. */
@Component({
  selector: 'app-import-bar',
  imports: [MatButtonModule, MatProgressBarModule],
  templateUrl: './import-bar.html',
  styleUrl: './import-bar.scss',
})
export class ImportBar {
  private readonly api = inject(TaxApiService);

  readonly summary = input<ImportSummary | null>(null);
  readonly uploaded = output<ImportResult>();

  readonly uploading = signal(false);
  readonly lastResult = signal<ImportResult | null>(null);
  readonly error = signal<string | null>(null);

  protected readonly formatTimestamp = formatTimestamp;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // so choosing the same file again still triggers a change
    if (file) this.upload(file);
  }

  upload(file: File): void {
    if (this.uploading()) return;

    this.error.set(null);
    this.lastResult.set(null);
    if (file.size === 0) return this.error.set('The file is empty.');
    if (file.size > MAX_FILE_BYTES) return this.error.set('The file must be 10 MB or smaller.');

    this.uploading.set(true);
    this.api.uploadCsv(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.lastResult.set(result);
        this.uploaded.emit(result);
      },
      error: (err: HttpErrorResponse) => {
        this.uploading.set(false);
        this.error.set(describeError(err));
      },
    });
  }
}

function describeError(err: HttpErrorResponse): string {
  if (err.status === 400) return err.error?.detail ?? err.error?.title ?? 'The file could not be imported.';
  if (err.status === 0) return 'Could not reach the server. Try again.';
  return `The upload failed (HTTP ${err.status}). Try again.`;
}
