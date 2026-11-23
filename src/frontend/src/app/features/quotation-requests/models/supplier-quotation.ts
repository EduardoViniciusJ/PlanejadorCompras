import { CommercialTermsDto, SupplierQuotationStatus } from '../dtos/quotation-request.dto';

export type StatusTone = 'neutral' | 'info' | 'success' | 'danger';

export interface SupplierQuotationStatusView {
  readonly label: string;
  readonly icon: string;
  readonly tone: StatusTone;
}

interface StatusSource {
  readonly status: SupplierQuotationStatus;
  readonly isLinkActive: boolean;
}

const STATUS_VIEWS: Record<SupplierQuotationStatus, SupplierQuotationStatusView> = {
  sent: { label: 'Enviada', icon: 'send', tone: 'neutral' },
  viewed: { label: 'Vista', icon: 'eye', tone: 'info' },
  responded: { label: 'Respondida', icon: 'circle-check', tone: 'success' },
};

/** Texto, ícone e cor do status de um fornecedor na solicitação. */
export function supplierQuotationStatusView(source: StatusSource): SupplierQuotationStatusView {
  return STATUS_VIEWS[source.status] ?? STATUS_VIEWS.sent;
}

/** Data que explica o status atual: quando respondeu, quando viu ou quando foi enviada. */
export function supplierQuotationStatusDate(source: {
  readonly sentAtUtc: string;
  readonly viewedAtUtc: string | null;
  readonly respondedAtUtc: string | null;
}): string {
  return source.respondedAtUtc ?? source.viewedAtUtc ?? source.sentAtUtc;
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(
    new Date(value),
  );
}

/** Datas sem hora (yyyy-MM-dd) são lidas como dia local, sem deslocar pelo fuso. */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR').format(new Date(year, month - 1, day));
}

const currencyFormatter = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatDeliveryDays(days: number): string {
  if (days === 0) return 'Imediata';
  return days === 1 ? '1 dia' : `${days} dias`;
}

export function formatFreight(freightCost: number): string {
  return freightCost === 0 ? 'Grátis' : formatCurrency(freightCost);
}

export interface CommercialTermLine {
  readonly label: string;
  readonly value: string;
}

/** Condições comerciais informadas, prontas para exibir em lista. */
export function commercialTermLines(terms: CommercialTermsDto | null): readonly CommercialTermLine[] {
  if (!terms) return [];

  const lines: CommercialTermLine[] = [];
  if (terms.deliveryDays !== null) {
    lines.push({ label: 'Prazo de entrega', value: formatDeliveryDays(terms.deliveryDays) });
  }
  if (terms.paymentTerms) {
    lines.push({ label: 'Pagamento', value: terms.paymentTerms });
  }
  if (terms.freightCost !== null) {
    lines.push({ label: 'Frete', value: formatFreight(terms.freightCost) });
  }
  if (terms.minimumOrderValue !== null) {
    lines.push({ label: 'Pedido mínimo', value: formatCurrency(terms.minimumOrderValue) });
  }
  if (terms.proposalValidUntil) {
    lines.push({ label: 'Proposta válida até', value: formatDateOnly(terms.proposalValidUntil) });
  }
  if (terms.notes) {
    lines.push({ label: 'Observações', value: terms.notes });
  }
  return lines;
}

export interface SupplierMessageContext {
  readonly code: string;
  readonly listName: string;
  readonly responseDeadline: string | null;
  readonly linkUrl: string;
}

/** Mensagem enviada ao fornecedor pelo WhatsApp, já com o link para responder. */
export function buildSupplierMessage(context: SupplierMessageContext): string {
  const deadline = context.responseDeadline
    ? ` Responder até ${formatDateOnly(context.responseDeadline)}.`
    : '';

  return (
    `Olá! Segue a solicitação de cotação ${context.code} (${context.listName}).${deadline} ` +
    `Responda por este link, sem precisar de cadastro: ${context.linkUrl}`
  );
}

/**
 * Endereço do WhatsApp com a mensagem pronta. Com telefone brasileiro (DDD + número) a
 * conversa já abre no contato; sem telefone, o WhatsApp pede para escolher o destinatário.
 */
export function buildWhatsAppUrl(phone: string | null, message: string): string {
  const digits = (phone ?? '').replace(/\D/g, '');
  const recipient =
    digits.length === 10 || digits.length === 11
      ? `55${digits}`
      : digits.length === 12 || digits.length === 13
        ? digits
        : '';

  return `https://wa.me/${recipient}?text=${encodeURIComponent(message)}`;
}
