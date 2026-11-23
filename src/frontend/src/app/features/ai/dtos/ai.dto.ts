export interface AiStatusDto {
  readonly enabled: boolean;
  readonly used: number;
  readonly limit: number;
  readonly remaining: number;
  /** First day of the next period, as yyyy-MM-dd. */
  readonly renewsOn: string;
  readonly maxFileBytes: number;
  readonly maxInputCharacters: number;
}

export type AiMatchConfidence = 'high' | 'medium' | 'low' | 'none';

export interface ExtractedQuoteLineDto {
  readonly lineNumber: number;
  readonly supplierDescription: string;
  readonly supplierUnit: string | null;
  readonly supplierUnitPrice: number | null;
  readonly shoppingItemId: string | null;
  readonly unitPrice: number;
  readonly confidence: AiMatchConfidence;
  readonly note: string | null;
}

export interface QuoteExtractionResponseDto {
  readonly lines: readonly ExtractedQuoteLineDto[];
  readonly generalConditions: string | null;
  readonly discardedLines: number;
  readonly usage: AiStatusDto;
}

export interface QuoteBatchLineDto {
  readonly shoppingItemId: string;
  readonly unitPrice: number;
}

export interface QuoteBatchRequestDto {
  readonly supplierId: string;
  readonly quotes: readonly QuoteBatchLineDto[];
}

export interface QuoteBatchResponseDto {
  readonly created: number;
  readonly updated: number;
}

export interface DraftShoppingItemDto {
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface ShoppingListDraftResponseDto {
  readonly suggestedName: string;
  readonly items: readonly DraftShoppingItemDto[];
  readonly discardedItems: number;
  readonly usage: AiStatusDto;
}
