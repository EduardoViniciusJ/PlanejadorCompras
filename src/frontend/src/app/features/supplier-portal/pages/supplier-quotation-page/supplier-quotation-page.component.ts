import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { BrandMarkComponent } from '../../../../shared/ui/brand-mark/brand-mark.component';
import {
  formatCurrency,
  formatDateOnly,
  formatDateTime,
} from '../../../quotation-requests/models/supplier-quotation';
import { SupplierPortalService } from '../../data-access/supplier-portal.service';
import {
  PublicQuotationItemDto,
  PublicSupplierQuotationDto,
  SubmitSupplierQuotationDto,
} from '../../dtos/supplier-portal.dto';
import { MoneyParseResult, formatMoneyInput, parseMoney } from '../../models/money-input';

interface LoadFailure {
  readonly title: string;
  readonly message: string;
  readonly canRetry: boolean;
}

const MAXIMUM_DELIVERY_DAYS = 365;
const PRICE_FORMAT_ERROR = 'Use um valor como 12,50.';

/**
 * Página que o fornecedor abre pelo link recebido, sem login. Mostra só a cotação dele e
 * permite informar preços e condições até o prazo. Pensada primeiro para o celular.
 */
@Component({
  selector: 'app-supplier-quotation-page',
  imports: [AppIconComponent, BrandMarkComponent],
  templateUrl: './supplier-quotation-page.component.html',
  styleUrl: './supplier-quotation-page.component.scss',
})
export class SupplierQuotationPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly service = inject(SupplierPortalService);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);
  private readonly token = this.route.snapshot.paramMap.get('token') ?? '';

  protected readonly quotation = signal<PublicSupplierQuotationDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly loadFailure = signal<LoadFailure | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly justSent = signal(false);
  /** Erros de campo só aparecem depois da primeira tentativa de envio ou ao sair do campo. */
  protected readonly showAllErrors = signal(false);
  protected readonly touchedPrices = signal<ReadonlySet<string>>(new Set());

  protected readonly priceTexts = signal<Readonly<Record<string, string>>>({});
  protected readonly deliveryDays = signal('');
  protected readonly paymentTerms = signal('');
  protected readonly freightCost = signal('');
  protected readonly minimumOrderValue = signal('');
  protected readonly proposalValidUntil = signal('');
  protected readonly notes = signal('');
  protected readonly today = toLocalDate(new Date());
  protected readonly maximumDeliveryDays = MAXIMUM_DELIVERY_DAYS;

  protected readonly canRespond = computed(() => this.quotation()?.canRespond ?? false);
  protected readonly parsedPrices = computed(() => {
    const texts = this.priceTexts();
    const parsed = new Map<string, MoneyParseResult>();
    for (const item of this.quotation()?.items ?? []) {
      parsed.set(item.id, parseMoney(texts[item.id] ?? ''));
    }
    return parsed;
  });
  protected readonly pricedCount = computed(
    () => [...this.parsedPrices().values()].filter((price) => price.kind === 'value').length,
  );
  protected readonly itemsTotal = computed(() =>
    (this.quotation()?.items ?? []).reduce((total, item) => {
      const price = this.parsedPrices().get(item.id);
      return price?.kind === 'value' ? total + price.value * item.quantity : total;
    }, 0),
  );
  protected readonly parsedFreight = computed(() => parseMoney(this.freightCost()));
  protected readonly parsedMinimumOrder = computed(() => parseMoney(this.minimumOrderValue()));
  protected readonly grandTotal = computed(() => {
    const freight = this.parsedFreight();
    return this.itemsTotal() + (freight.kind === 'value' ? freight.value : 0);
  });
  protected readonly deliveryDaysError = computed(() => {
    const text = this.deliveryDays().trim();
    if (!text) return null;
    return /^\d{1,3}$/.test(text) && Number(text) <= MAXIMUM_DELIVERY_DAYS
      ? null
      : `Informe um número de dias entre 0 e ${MAXIMUM_DELIVERY_DAYS}.`;
  });
  protected readonly freightError = computed(() =>
    this.parsedFreight().kind === 'invalid' ? PRICE_FORMAT_ERROR : null,
  );
  protected readonly minimumOrderError = computed(() =>
    this.parsedMinimumOrder().kind === 'invalid' ? PRICE_FORMAT_ERROR : null,
  );
  protected readonly validUntilError = computed(() => {
    const value = this.proposalValidUntil();
    return value && value < this.today ? 'A validade não pode ser uma data que já passou.' : null;
  });
  protected readonly hasPriceErrors = computed(() =>
    (this.quotation()?.items ?? []).some((item) => this.priceProblem(item.id) !== null),
  );

  ngOnInit(): void {
    // O endereço desta página carrega o token: nunca deve seguir como "referer".
    this.meta.updateTag({ name: 'referrer', content: 'no-referrer' });
    this.destroyRef.onDestroy(() => this.meta.removeTag('name="referrer"'));
    this.load();
  }

  protected retry(): void {
    this.load();
  }

  protected updatePrice(itemId: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.priceTexts.update((texts) => ({ ...texts, [itemId]: value }));
    this.justSent.set(false);
  }

  /** Ao sair do campo, o valor é reescrito no formato padrão (12,50) e validado. */
  protected normalizePrice(itemId: string): void {
    this.touchedPrices.update((touched) => new Set(touched).add(itemId));
    const parsed = this.parsedPrices().get(itemId);
    if (parsed?.kind === 'value' && parsed.value > 0) {
      this.priceTexts.update((texts) => ({ ...texts, [itemId]: formatMoneyInput(parsed.value) }));
    }
  }

  protected normalizeAmount(field: 'freight' | 'minimumOrder'): void {
    const target = field === 'freight' ? this.freightCost : this.minimumOrderValue;
    const parsed = parseMoney(target());
    if (parsed.kind === 'value') target.set(formatMoneyInput(parsed.value));
  }

  protected updateText(
    field: 'deliveryDays' | 'paymentTerms' | 'freightCost' | 'minimumOrderValue' | 'proposalValidUntil' | 'notes',
    event: Event,
  ): void {
    this[field].set((event.target as HTMLInputElement | HTMLTextAreaElement).value);
    this.justSent.set(false);
  }

  protected priceError(itemId: string): string | null {
    if (!this.showAllErrors() && !this.touchedPrices().has(itemId)) return null;
    return this.priceProblem(itemId);
  }

  protected lineTotal(item: PublicQuotationItemDto): string | null {
    const price = this.parsedPrices().get(item.id);
    return price?.kind === 'value' && price.value > 0
      ? formatCurrency(price.value * item.quantity)
      : null;
  }

  protected submit(): void {
    const quotation = this.quotation();
    if (!quotation || !quotation.canRespond || this.isSubmitting()) return;

    this.showAllErrors.set(true);
    this.submitError.set(null);
    this.justSent.set(false);

    if (this.pricedCount() === 0 && !this.hasPriceErrors()) {
      this.submitError.set('Informe o preço de pelo menos um item para enviar a cotação.');
      return;
    }

    if (
      this.hasPriceErrors() ||
      this.deliveryDaysError() ||
      this.freightError() ||
      this.minimumOrderError() ||
      this.validUntilError()
    ) {
      this.submitError.set('Há campos com problema. Corrija os valores destacados e envie de novo.');
      return;
    }

    this.isSubmitting.set(true);
    this.service
      .respond(this.token, this.buildResponse(quotation))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          this.applyQuotation(updated);
          this.justSent.set(true);
          this.document.defaultView?.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          this.handleSubmitError(error);
        },
      });
  }

  protected formatCurrency(value: number): string {
    return formatCurrency(value);
  }

  protected formatDate(value: string): string {
    return formatDateOnly(value);
  }

  protected formatDateTime(value: string): string {
    return formatDateTime(value);
  }

  private priceProblem(itemId: string): string | null {
    const parsed = this.parsedPrices().get(itemId);
    if (!parsed || parsed.kind === 'empty') return null;
    if (parsed.kind === 'invalid') return PRICE_FORMAT_ERROR;
    return parsed.value > 0 ? null : 'O preço deve ser maior que zero. Deixe em branco se não tiver o item.';
  }

  private buildResponse(quotation: PublicSupplierQuotationDto): SubmitSupplierQuotationDto {
    const prices = quotation.items.flatMap((item) => {
      const price = this.parsedPrices().get(item.id);
      return price?.kind === 'value' ? [{ itemId: item.id, unitPrice: price.value }] : [];
    });
    const freight = this.parsedFreight();
    const minimumOrder = this.parsedMinimumOrder();
    const deliveryDays = this.deliveryDays().trim();

    return {
      prices,
      deliveryDays: deliveryDays ? Number(deliveryDays) : null,
      paymentTerms: this.paymentTerms().trim() || null,
      freightCost: freight.kind === 'value' ? freight.value : null,
      minimumOrderValue: minimumOrder.kind === 'value' ? minimumOrder.value : null,
      proposalValidUntil: this.proposalValidUntil() || null,
      notes: this.notes().trim() || null,
    };
  }

  private load(): void {
    this.isLoading.set(true);
    this.loadFailure.set(null);
    this.service
      .get(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (quotation) => {
          this.applyQuotation(quotation);
          this.isLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.loadFailure.set(describeLoadFailure(error));
        },
      });
  }

  private applyQuotation(quotation: PublicSupplierQuotationDto): void {
    this.quotation.set(quotation);
    this.priceTexts.set(
      Object.fromEntries(
        quotation.items.map((item) => [
          item.id,
          item.unitPrice === null ? '' : formatMoneyInput(item.unitPrice),
        ]),
      ),
    );
    const terms = quotation.terms;
    this.deliveryDays.set(terms?.deliveryDays?.toString() ?? '');
    this.paymentTerms.set(terms?.paymentTerms ?? '');
    this.freightCost.set(
      terms?.freightCost === null || terms?.freightCost === undefined
        ? ''
        : formatMoneyInput(terms.freightCost),
    );
    this.minimumOrderValue.set(
      terms?.minimumOrderValue === null || terms?.minimumOrderValue === undefined
        ? ''
        : formatMoneyInput(terms.minimumOrderValue),
    );
    this.proposalValidUntil.set(terms?.proposalValidUntil ?? '');
    this.notes.set(terms?.notes ?? '');
    this.showAllErrors.set(false);
    this.touchedPrices.set(new Set());
  }

  private handleSubmitError(error: HttpErrorResponse): void {
    const errorCode = (error.error as { errorCode?: string } | null)?.errorCode;
    if (errorCode === 'quotation_response_deadline_passed') {
      this.submitError.set('O prazo para responder terminou. A cotação não pode mais ser enviada nem alterada.');
      this.quotation.update((quotation) => (quotation ? { ...quotation, canRespond: false } : quotation));
      return;
    }

    if (error.status === 404 || error.status === 410) {
      this.quotation.set(null);
      this.loadFailure.set(describeLoadFailure(error));
      return;
    }

    this.submitError.set(
      error.status === 429
        ? 'Muitos envios em pouco tempo. Aguarde um minuto e tente de novo.'
        : error.status === 400
          ? 'Algum valor não foi aceito. Revise preços e condições e envie de novo.'
          : 'Não foi possível enviar agora. Verifique a conexão e tente de novo. Seus valores continuam na tela.',
    );
  }
}

function describeLoadFailure(error: HttpErrorResponse): LoadFailure {
  const errorCode = (error.error as { errorCode?: string } | null)?.errorCode;
  if (errorCode === 'quotation_link_revoked') {
    return {
      title: 'Este link foi cancelado',
      message: 'O comprador cancelou este link. Se ainda quiser cotar, peça um link novo a quem enviou.',
      canRetry: false,
    };
  }

  if (errorCode === 'quotation_link_expired') {
    return {
      title: 'Este link expirou',
      message: 'O período desta cotação terminou. Para participar, peça um link novo ao comprador.',
      canRetry: false,
    };
  }

  if (error.status === 404) {
    return {
      title: 'Link não encontrado',
      message: 'Confira se o endereço está completo, igual ao que você recebeu. Se continuar, peça o link de novo ao comprador.',
      canRetry: false,
    };
  }

  if (error.status === 429) {
    return {
      title: 'Muitas tentativas',
      message: 'Aguarde um minuto e tente abrir de novo.',
      canRetry: true,
    };
  }

  return {
    title: 'Não foi possível abrir a cotação',
    message: 'Verifique a sua conexão e tente de novo.',
    canRetry: true,
  };
}

function toLocalDate(value: Date): string {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
