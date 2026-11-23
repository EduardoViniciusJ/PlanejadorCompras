import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { buildSupplierMessage, buildWhatsAppUrl } from '../../models/supplier-quotation';

/**
 * Link público de um fornecedor, com atalhos para copiar e enviar pelo WhatsApp.
 * Só aparece logo depois de o link ser criado ou trocado, enquanto o endereço está na tela.
 */
@Component({
  selector: 'app-supplier-link-actions',
  imports: [AppIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="supplier-link">
      <label class="input input-sm supplier-link-field">
        <app-icon name="link" [size]="16" />
        <span class="visually-hidden">Link de resposta de {{ supplierName() }}</span>
        <input
          class="grow"
          type="text"
          readonly
          [value]="linkUrl()"
          (focus)="selectAll($event)"
          data-testid="supplier-link-url"
        />
      </label>
      <button class="btn btn-sm" type="button" (click)="copy()" data-testid="copy-supplier-link">
        <app-icon [name]="copied() ? 'check' : 'copy'" [size]="16" />
        {{ copied() ? 'Copiado' : 'Copiar link' }}
      </button>
      <a
        class="btn btn-sm whatsapp-button"
        target="_blank"
        rel="noopener noreferrer"
        [href]="whatsAppUrl()"
        data-testid="whatsapp-supplier-link"
      >
        <app-icon name="brand-whatsapp" [size]="16" /> WhatsApp
      </a>
    </div>
    @if (copyFailed()) {
      <p class="supplier-link-hint" role="status">
        Não foi possível copiar automaticamente. Selecione o link e copie manualmente.
      </p>
    }
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    .supplier-link {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.4rem;
    }

    .supplier-link-field {
      flex: 1 1 14rem;
      min-width: 0;
    }

    .supplier-link-field input {
      min-width: 0;
      font-size: 0.78rem;
      text-overflow: ellipsis;
    }

    .whatsapp-button {
      border-color: color-mix(in srgb, var(--app-success) 48%, var(--app-border-color));
      background: var(--app-success-soft);
      color: var(--app-success-strong);
    }

    .supplier-link-hint {
      margin: 0.35rem 0 0;
      color: var(--app-text-muted);
      font-size: 0.78rem;
    }

    @media (max-width: 560px) {
      .supplier-link .btn {
        flex: 1 1 8rem;
      }
    }
  `,
})
export class SupplierLinkActionsComponent {
  private readonly document = inject(DOCUMENT);

  readonly linkUrl = input.required<string>();
  readonly supplierName = input.required<string>();
  readonly supplierPhone = input<string | null>(null);
  readonly code = input.required<string>();
  readonly listName = input.required<string>();
  readonly responseDeadline = input<string | null>(null);

  protected readonly copied = signal(false);
  protected readonly copyFailed = signal(false);
  protected readonly whatsAppUrl = computed(() =>
    buildWhatsAppUrl(
      this.supplierPhone(),
      buildSupplierMessage({
        code: this.code(),
        listName: this.listName(),
        responseDeadline: this.responseDeadline(),
        linkUrl: this.linkUrl(),
      }),
    ),
  );

  protected selectAll(event: Event): void {
    (event.target as HTMLInputElement).select();
  }

  protected copy(): void {
    const clipboard = this.document.defaultView?.navigator.clipboard;
    if (!clipboard) {
      this.copyFailed.set(true);
      return;
    }

    clipboard.writeText(this.linkUrl()).then(
      () => {
        this.copied.set(true);
        this.copyFailed.set(false);
      },
      () => this.copyFailed.set(true),
    );
  }
}
