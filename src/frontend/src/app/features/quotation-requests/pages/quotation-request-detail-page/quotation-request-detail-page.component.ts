import { DOCUMENT } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ModalDialogComponent } from '../../../../shared/ui/modal-dialog/modal-dialog.component';
import { SkeletonComponent } from '../../../../shared/ui/skeleton/skeleton.component';
import { ShoppingListReportFile } from '../../../reports/models/shopping-list-report-file.model';
import { ReportFileDownloadService } from '../../../reports/services/report-file-download.service';
import { SupplierLinkActionsComponent } from '../../components/supplier-link-actions/supplier-link-actions.component';
import { SupplierStatusChipComponent } from '../../components/supplier-status-chip/supplier-status-chip.component';
import { QuotationRequestService } from '../../data-access/quotation-request.service';
import {
  QuotationRequestDetailDto,
  QuotationRequestItemDto,
  SupplierQuotationDto,
} from '../../dtos/quotation-request.dto';
import {
  CommercialTermLine,
  commercialTermLines,
  formatCurrency,
  formatDateOnly,
  formatDateTime,
} from '../../models/supplier-quotation';

type SupplierAction = 'renew' | 'renew-email' | 'revoke';

interface PendingSupplierAction {
  readonly supplier: SupplierQuotationDto;
  readonly action: 'renew' | 'revoke';
}

interface SupplierResponseRow {
  readonly item: QuotationRequestItemDto;
  readonly unitPrice: number | null;
  readonly total: number | null;
}

@Component({
  selector: 'app-quotation-request-detail-page',
  imports: [
    RouterLink,
    AppIconComponent,
    SkeletonComponent,
    ModalDialogComponent,
    SupplierLinkActionsComponent,
    SupplierStatusChipComponent,
  ],
  templateUrl: './quotation-request-detail-page.component.html',
  styleUrl: './quotation-request-detail-page.component.scss',
})
export class QuotationRequestDetailPageComponent implements OnInit {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(QuotationRequestService);
  private readonly downloadService = inject(ReportFileDownloadService);
  private readonly requestId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly request = signal<QuotationRequestDetailDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isPreparingPdf = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly feedback = signal<string | null>(null);
  protected readonly emailAvailable = signal(false);
  protected readonly pendingAction = signal<PendingSupplierAction | null>(null);
  protected readonly isUpdatingSupplier = signal(false);
  protected readonly supplierActionError = signal<string | null>(null);
  /** Links gerados nesta visita. O servidor não devolve o endereço de novo depois. */
  protected readonly freshLinks = signal<Readonly<Record<string, string>>>({});

  ngOnInit(): void {
    this.load();
    this.service
      .getDeliveryOptions()
      .pipe(
        catchError(() => of({ emailAvailable: false })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((options) => this.emailAvailable.set(options.emailAvailable));
  }

  protected retry(): void { this.load(); }

  protected downloadPdf(): void {
    this.withPdf((file) => {
      this.downloadService.download(file);
      this.feedback.set('PDF baixado com sucesso.');
    });
  }

  protected share(): void {
    this.withPdf((file) => void this.shareFile(file));
  }

  protected openSupplierAction(supplier: SupplierQuotationDto, action: 'renew' | 'revoke'): void {
    this.supplierActionError.set(null);
    this.pendingAction.set({ supplier, action });
  }

  protected closeSupplierAction(): void {
    if (this.isUpdatingSupplier()) return;
    this.pendingAction.set(null);
    this.supplierActionError.set(null);
  }

  protected confirmSupplierAction(action: SupplierAction): void {
    const pending = this.pendingAction();
    if (!pending || this.isUpdatingSupplier()) return;

    const supplier = pending.supplier;
    const operation =
      action === 'revoke'
        ? this.service.revokeSupplierLink(this.requestId, supplier.id)
        : this.service.renewSupplierLink(this.requestId, supplier.id, action === 'renew-email');

    this.isUpdatingSupplier.set(true);
    this.supplierActionError.set(null);
    this.feedback.set(null);
    this.actionError.set(null);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.isUpdatingSupplier.set(false);
        this.pendingAction.set(null);
        this.replaceSupplier(updated);
        this.freshLinks.update((links) => {
          const next = { ...links };
          if (updated.linkUrl) next[updated.id] = updated.linkUrl;
          else delete next[updated.id];
          return next;
        });
        this.reportSupplierAction(action, updated);
      },
      error: (error: { error?: { errorCode?: string } }) => {
        this.isUpdatingSupplier.set(false);
        this.supplierActionError.set(supplierActionErrorMessage(error.error?.errorCode));
      },
    });
  }

  protected linkState(supplier: SupplierQuotationDto): string {
    if (supplier.isLinkRevoked) return 'Link revogado: o fornecedor não consegue mais abrir.';
    if (!supplier.isLinkActive) return `Link expirado em ${formatDateTime(supplier.linkExpiresAtUtc)}.`;
    return `Link ativo até ${formatDateTime(supplier.linkExpiresAtUtc)}.`;
  }

  protected responseRows(
    request: QuotationRequestDetailDto,
    supplier: SupplierQuotationDto,
  ): readonly SupplierResponseRow[] {
    const prices = new Map(supplier.prices.map((price) => [price.itemId, price.unitPrice]));
    return request.items.map((item) => {
      const unitPrice = prices.get(item.id) ?? null;
      return { item, unitPrice, total: unitPrice === null ? null : unitPrice * item.quantity };
    });
  }

  protected responseTotal(rows: readonly SupplierResponseRow[]): number {
    return rows.reduce((total, row) => total + (row.total ?? 0), 0);
  }

  protected termLines(supplier: SupplierQuotationDto): readonly CommercialTermLine[] {
    return commercialTermLines(supplier.terms);
  }

  protected formatCurrency(value: number): string {
    return formatCurrency(value);
  }

  protected formatDateTime(value: string): string {
    return formatDateTime(value);
  }

  protected formatDate(value: string | null): string {
    return value ? formatDateOnly(value) : 'Não informado';
  }

  private replaceSupplier(updated: SupplierQuotationDto): void {
    this.request.update((request) =>
      request
        ? {
            ...request,
            suppliers: request.suppliers.map((supplier) =>
              supplier.id === updated.id ? updated : supplier,
            ),
          }
        : request,
    );
  }

  private reportSupplierAction(action: SupplierAction, supplier: SupplierQuotationDto): void {
    if (action === 'revoke') {
      this.feedback.set(`Link de ${supplier.supplierName} revogado.`);
      return;
    }

    if (action === 'renew-email' && supplier.emailDelivery !== 'sent') {
      this.actionError.set(
        `O link novo de ${supplier.supplierName} foi gerado, mas o e-mail não pôde ser enviado. ` +
          'Copie o link ou envie pelo WhatsApp.',
      );
      return;
    }

    this.feedback.set(
      action === 'renew-email'
        ? `Link novo enviado por e-mail para ${supplier.supplierName}. O link anterior não funciona mais.`
        : `Link novo gerado para ${supplier.supplierName}. O link anterior não funciona mais.`,
    );
  }

  private load(): void {
    if (!this.requestId) {
      this.isLoading.set(false);
      this.loadError.set('Solicitação inválida.');
      return;
    }
    this.isLoading.set(true);
    this.loadError.set(null);
    this.service.getById(this.requestId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (request) => { this.request.set(request); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.loadError.set('Não foi possível carregar esta solicitação.'); },
    });
  }

  private withPdf(action: (file: ShoppingListReportFile) => void): void {
    if (!this.request() || this.isPreparingPdf()) return;
    this.isPreparingPdf.set(true);
    this.actionError.set(null);
    this.service.downloadPdf(this.requestId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (file) => {
        try { action(file); }
        catch { this.actionError.set('Não foi possível usar o arquivo PDF.'); }
        finally { this.isPreparingPdf.set(false); }
      },
      error: () => { this.isPreparingPdf.set(false); this.actionError.set('Não foi possível gerar o PDF.'); },
    });
  }

  private async shareFile(report: ShoppingListReportFile): Promise<void> {
    const request = this.request();
    const targetWindow = this.document.defaultView;
    if (!request || !targetWindow) return;
    const file = new File([report.content], report.fileName, { type: 'application/pdf' });
    if (targetWindow.navigator.share && (!targetWindow.navigator.canShare || targetWindow.navigator.canShare({ files: [file] }))) {
      try {
        await targetWindow.navigator.share({
          title: `Solicitação de cotação ${request.code}`,
          text: `Segue a solicitação de cotação para ${request.shoppingListName}.`,
          files: [file],
        });
        this.feedback.set('Solicitação compartilhada.');
        return;
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') return;
      }
    }
    this.downloadService.download(report);
    const message = encodeURIComponent(`Olá! Segue a solicitação de cotação ${request.code} para “${request.shoppingListName}”. O PDF foi baixado para ser anexado nesta conversa.`);
    targetWindow.open(`https://wa.me/?text=${message}`, '_blank', 'noopener,noreferrer');
    this.feedback.set('O WhatsApp foi aberto e o PDF foi baixado para anexar.');
  }
}

function supplierActionErrorMessage(errorCode: string | undefined): string {
  switch (errorCode) {
    case 'email_delivery_unavailable':
      return 'O envio por e-mail não está configurado neste ambiente. Gere o link e envie pelo WhatsApp.';
    case 'supplier_without_email':
      return 'Este fornecedor não tem e-mail cadastrado. Gere o link e envie pelo WhatsApp.';
    case 'rate_limited':
      return 'Muitas tentativas em pouco tempo. Aguarde alguns minutos e tente de novo.';
    default:
      return 'Não foi possível concluir agora. Tente novamente.';
  }
}
