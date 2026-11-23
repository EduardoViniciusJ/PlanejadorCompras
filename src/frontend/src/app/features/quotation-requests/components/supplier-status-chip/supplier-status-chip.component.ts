import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { SupplierQuotationStatus } from '../../dtos/quotation-request.dto';
import { supplierQuotationStatusView } from '../../models/supplier-quotation';

/** Etiqueta com o status da cotação de um fornecedor: enviada, vista ou respondida. */
@Component({
  selector: 'app-supplier-status-chip',
  imports: [AppIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="status-chip" [attr.data-tone]="view().tone" [attr.data-status]="status()">
      <app-icon [name]="view().icon" [size]="13" />
      {{ view().label }}
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .status-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      padding: 0.24rem 0.5rem;
      border: 1px solid var(--chip-color, var(--app-border-color));
      border-radius: var(--app-control-radius);
      background: var(--chip-bg, var(--app-surface-alt-bg));
      color: var(--chip-color, var(--app-text-muted));
      font-size: 0.7rem;
      font-weight: 800;
      line-height: 1.15;
      white-space: nowrap;
    }

    .status-chip[data-tone='info'] {
      --chip-color: var(--app-info-strong);
      --chip-bg: var(--app-info-soft);
    }

    .status-chip[data-tone='success'] {
      --chip-color: var(--app-success-strong);
      --chip-bg: var(--app-success-soft);
    }
  `,
})
export class SupplierStatusChipComponent {
  readonly status = input.required<SupplierQuotationStatus>();
  readonly isLinkActive = input(true);

  protected readonly view = computed(() =>
    supplierQuotationStatusView({ status: this.status(), isLinkActive: this.isLinkActive() }),
  );
}
