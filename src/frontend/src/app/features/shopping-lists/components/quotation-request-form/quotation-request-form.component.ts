import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { SupplierLinkActionsComponent } from '../../../quotation-requests/components/supplier-link-actions/supplier-link-actions.component';
import {
  EmailDelivery,
  QuotationRequestDetailDto,
} from '../../../quotation-requests/dtos/quotation-request.dto';
import { Supplier } from '../../../suppliers/models/supplier.model';
import { QuotationRequestPdfRequestDto } from '../../dtos/quotation-request.dto';

const MAXIMUM_SUPPLIERS = 30;

const EMAIL_DELIVERY_LABELS: Record<EmailDelivery, string> = {
  sent: 'E-mail enviado',
  failed: 'O e-mail não pôde ser enviado. Use o link.',
  no_address: 'Sem e-mail cadastrado. Use o link.',
  unavailable: 'Envio por e-mail indisponível. Use o link.',
};

@Component({
  selector: 'app-quotation-request-form',
  imports: [ReactiveFormsModule, AppIconComponent, SupplierLinkActionsComponent],
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
  /** Fornecedores cadastrados que podem receber uma cotação própria. */
  readonly suppliers = input<readonly Supplier[]>([]);
  /** Fornecedores já vinculados à lista: chegam marcados. */
  readonly preselectedSupplierIds = input<readonly string[]>([]);
  readonly suppliersLoading = input(false);
  /** `false` quando o servidor não tem provedor de e-mail configurado. */
  readonly emailAvailable = input(false);
  /** Solicitação recém-criada, com o link de cada fornecedor. */
  readonly createdRequest = input<QuotationRequestDetailDto | null>(null);
  readonly generateRequested = output<QuotationRequestPdfRequestDto>();
  readonly downloadRequested = output<void>();
  readonly shareRequested = output<void>();
  readonly viewRequested = output<void>();
  readonly cancelRequested = output<void>();

  // Valor do <input type="date">: texto ISO yyyy-MM-dd (mesmo formato enviado à API).
  protected readonly minDeadline = toLocalDate(new Date());
  protected readonly maximumSuppliers = MAXIMUM_SUPPLIERS;
  protected readonly form = this.formBuilder.group({
    responseDeadline: this.formBuilder.control<string | null>(null),
    deliveryAddress: ['', []],
    instructions: ['', []],
  });
  protected readonly selectedSupplierIds = linkedSignal<ReadonlySet<string>>(
    () => new Set(this.preselectedSupplierIds().slice(0, MAXIMUM_SUPPLIERS)),
  );
  protected readonly sendEmail = signal(false);
  protected readonly selectedCount = computed(() => this.selectedSupplierIds().size);
  protected readonly selectedWithoutEmail = computed(
    () =>
      this.suppliers().filter(
        (supplier) => this.selectedSupplierIds().has(supplier.id) && !supplier.contact?.email,
      ).length,
  );
  protected readonly createdSuppliers = computed(() => this.createdRequest()?.suppliers ?? []);

  protected isSelected(supplierId: string): boolean {
    return this.selectedSupplierIds().has(supplierId);
  }

  protected canSelect(supplierId: string): boolean {
    return this.isSelected(supplierId) || this.selectedCount() < MAXIMUM_SUPPLIERS;
  }

  protected toggleSupplier(supplierId: string, checked: boolean): void {
    this.selectedSupplierIds.update((current) => {
      const next = new Set(current);
      if (checked && next.size < MAXIMUM_SUPPLIERS) next.add(supplierId);
      else if (!checked) next.delete(supplierId);
      return next;
    });
  }

  protected toggleSendEmail(checked: boolean): void {
    this.sendEmail.set(checked);
  }

  protected emailDeliveryLabel(delivery: EmailDelivery | null): string | null {
    return delivery ? EMAIL_DELIVERY_LABELS[delivery] : null;
  }

  protected deadlineInPast(): boolean {
    const deadline = this.form.controls.responseDeadline.value;
    return !!deadline && deadline < this.minDeadline;
  }

  protected generate(): void {
    if (this.generating() || this.deadlineInPast()) return;
    const value = this.form.getRawValue();
    // Só entram fornecedores que continuam na lista exibida.
    const supplierIds = this.suppliers()
      .filter((supplier) => this.selectedSupplierIds().has(supplier.id))
      .map((supplier) => supplier.id);
    this.generateRequested.emit({
      responseDeadline: value.responseDeadline || null,
      deliveryAddress: optional(value.deliveryAddress),
      instructions: optional(value.instructions),
      supplierIds,
      sendEmail: this.emailAvailable() && this.sendEmail() && supplierIds.length > 0,
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
