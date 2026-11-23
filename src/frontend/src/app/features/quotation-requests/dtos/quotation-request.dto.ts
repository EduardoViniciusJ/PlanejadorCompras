import { QuotationRequestPdfRequestDto } from '../../shopping-lists/dtos/quotation-request.dto';

export type CreateQuotationRequestDto = QuotationRequestPdfRequestDto;

export type SupplierQuotationStatus = 'sent' | 'viewed' | 'responded';

/** Resultado do envio por e-mail, devolvido só na resposta que tentou enviar. */
export type EmailDelivery = 'sent' | 'failed' | 'no_address' | 'unavailable';

export interface CommercialTermsDto {
  readonly deliveryDays: number | null;
  readonly paymentTerms: string | null;
  readonly freightCost: number | null;
  readonly minimumOrderValue: number | null;
  readonly proposalValidUntil: string | null;
  readonly notes: string | null;
}

export interface SupplierQuotationSummaryDto {
  readonly id: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly status: SupplierQuotationStatus;
  readonly sentAtUtc: string;
  readonly viewedAtUtc: string | null;
  readonly respondedAtUtc: string | null;
  readonly isLinkActive: boolean;
}

export interface SupplierQuotationPriceDto {
  readonly itemId: string;
  readonly unitPrice: number;
}

export interface SupplierQuotationDto {
  readonly id: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly supplierEmail: string | null;
  readonly supplierPhone: string | null;
  readonly status: SupplierQuotationStatus;
  readonly sentAtUtc: string;
  readonly emailSentAtUtc: string | null;
  readonly viewedAtUtc: string | null;
  readonly respondedAtUtc: string | null;
  readonly linkExpiresAtUtc: string;
  readonly isLinkRevoked: boolean;
  readonly isLinkActive: boolean;
  readonly terms: CommercialTermsDto | null;
  readonly prices: readonly SupplierQuotationPriceDto[];
  /**
   * Só vem preenchido logo depois de criar ou trocar o link: o servidor guarda apenas o
   * hash do token e não consegue mostrar o endereço de novo.
   */
  readonly linkUrl: string | null;
  readonly emailDelivery: EmailDelivery | null;
}

export interface QuotationDeliveryOptionsDto {
  readonly emailAvailable: boolean;
}

export interface QuotationRequestSummaryDto {
  readonly id: string;
  readonly code: string;
  readonly sourceShoppingListId: string | null;
  readonly shoppingListName: string;
  readonly buyerName: string;
  readonly itemCount: number;
  readonly responseDeadline: string | null;
  readonly createdAtUtc: string;
  readonly suppliers: readonly SupplierQuotationSummaryDto[];
}

export interface QuotationRequestItemDto {
  readonly id: string;
  readonly sourceShoppingItemId: string | null;
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface QuotationRequestDetailDto {
  readonly id: string;
  readonly code: string;
  readonly sourceShoppingListId: string | null;
  readonly shoppingListName: string;
  readonly description: string | null;
  readonly buyerName: string;
  readonly buyerEmail: string;
  readonly responseDeadline: string | null;
  readonly deliveryAddress: string | null;
  readonly instructions: string | null;
  readonly createdAtUtc: string;
  readonly items: readonly QuotationRequestItemDto[];
  readonly suppliers: readonly SupplierQuotationDto[];
}
