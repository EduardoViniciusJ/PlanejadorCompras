import { Component, computed, input, model, signal } from '@angular/core';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { AiStatusDto } from '../../dtos/ai.dto';
import { describeAiUsage, formatFileSize } from '../../models/ai-support';

/**
 * Text area plus file picker shared by the AI dialogs. It only collects the input and
 * checks size limits; the server validates the real file type again.
 */
@Component({
  selector: 'app-ai-source-input',
  imports: [AppIconComponent],
  templateUrl: './ai-source-input.component.html',
  styleUrl: './ai-source-input.component.scss',
})
export class AiSourceInputComponent {
  readonly status = input<AiStatusDto | null>(null);
  readonly textLabel = input.required<string>();
  readonly textPlaceholder = input('');
  readonly fileLabel = input.required<string>();
  /** Value for the file input "accept" attribute. */
  readonly accept = input.required<string>();
  readonly disabled = input(false);

  readonly text = model('');
  readonly file = model<File | null>(null);

  protected readonly fileError = signal<string | null>(null);

  protected readonly usage = computed(() => {
    const status = this.status();
    return status ? describeAiUsage(status) : null;
  });
  protected readonly maxCharacters = computed(() => this.status()?.maxInputCharacters ?? 20000);
  protected readonly textTooLong = computed(() => this.text().length > this.maxCharacters());
  protected readonly fileSizeLimit = computed(() =>
    formatFileSize(this.status()?.maxFileBytes ?? 5 * 1024 * 1024),
  );

  /** True when there is something valid to send. */
  readonly ready = computed(
    () =>
      !this.textTooLong() && !this.fileError() && (this.text().trim().length > 0 || !!this.file()),
  );

  protected updateText(event: Event): void {
    this.text.set((event.target as HTMLTextAreaElement).value);
  }

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    this.fileError.set(null);

    if (!selected) {
      this.file.set(null);
      return;
    }

    const maxFileBytes = this.status()?.maxFileBytes ?? 5 * 1024 * 1024;
    if (selected.size > maxFileBytes) {
      input.value = '';
      this.file.set(null);
      this.fileError.set(`O arquivo passa do limite de ${this.fileSizeLimit()}.`);
      return;
    }

    this.file.set(selected);
  }

  protected removeFile(input: HTMLInputElement): void {
    input.value = '';
    this.file.set(null);
    this.fileError.set(null);
  }

  protected describeFile(file: File): string {
    return `${file.name} (${formatFileSize(file.size)})`;
  }
}
