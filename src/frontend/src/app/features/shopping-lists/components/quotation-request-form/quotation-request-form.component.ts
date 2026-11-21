import { Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { QuotationRequestPdfRequestDto } from '../../dtos/quotation-request.dto';

@Component({
  selector: 'app-quotation-request-form',
  imports: [ReactiveFormsModule, AppIconComponent],
  templateUrl: './quotation-request-form.component.html',
  styleUrl: './quotation-request-form.component.scss',
})
export class QuotationRequestFormComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly listName = input.required<string>();
  readonly generating = input(false);
  readonly generated = input(false);
  readonly generatedCode = input<string | null>(null);
  readonly errorMessage = input<string | null>(null);
  readonly generateRequested = output<QuotationRequestPdfRequestDto>();
  readonly downloadRequested = output<void>();
  readonly shareRequested = output<void>();
  readonly viewRequested = output<void>();
  readonly cancelRequested = output<void>();

  // Valor do <input type="date">: texto ISO yyyy-MM-dd (mesmo formato enviado à API).
  protected readonly minDeadline = toLocalDate(new Date());
  protected readonly form = this.formBuilder.group({
    responseDeadline: this.formBuilder.control<string | null>(null),
    deliveryAddress: ['', []],
    instructions: ['', []],
  });

  protected deadlineInPast(): boolean {
    const deadline = this.form.controls.responseDeadline.value;
    return !!deadline && deadline < this.minDeadline;
  }

  protected generate(): void {
    if (this.generating() || this.deadlineInPast()) return;
    const value = this.form.getRawValue();
    this.generateRequested.emit({
      responseDeadline: value.responseDeadline || null,
      deliveryAddress: optional(value.deliveryAddress),
      instructions: optional(value.instructions),
    });
  }
}

function toLocalDate(value: Date): string {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function optional(value: string): string | null {
  const normalized = value.trim();
  return normalized || null;
}
