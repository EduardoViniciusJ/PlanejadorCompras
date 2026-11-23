import { CommercialTermsDto } from '../../quotation-requests/dtos/quotation-request.dto';

export interface PublicQuotationItemDto {
  readonly id: string;
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
  /** Preço que este fornecedor já informou; `null` se ainda não cotou o item. */
  readonly unitPrice: number | null;
}

/** O que o fornecedor vê pelo link público: só a cotação dele. */
export interface PublicSupplierQuotationDto {
  readonly code: string;
  readonly buyerName: string;
  readonly supplierName: string;
  readonly subject: string;
  readonly responseDeadline: string | null;
  readonly deliveryAddress: string | null;
  readonly instructions: string | null;
  /** `false` depois do prazo: a resposta fica só para leitura. */
  readonly canRespond: boolean;
  readonly respondedAtUtc: string | null;
  readonly terms: CommercialTermsDto | null;
  readonly items: readonly PublicQuotationItemDto[];
}

export interface SubmitSupplierQuotationDto {
  readonly prices: readonly { readonly itemId: string; readonly unitPrice: number }[];
  readonly deliveryDays: number | null;
  readonly paymentTerms: string | null;
  readonly freightCost: number | null;
  readonly minimumOrderValue: number | null;
  readonly proposalValidUntil: string | null;
  readonly notes: string | null;
}
