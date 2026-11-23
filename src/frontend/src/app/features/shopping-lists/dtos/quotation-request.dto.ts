export interface QuotationRequestPdfRequestDto {
  readonly responseDeadline: string | null;
  readonly deliveryAddress: string | null;
  readonly instructions: string | null;
  /** Fornecedores que recebem uma cotação própria, com link para responder. */
  readonly supplierIds?: readonly string[];
  /** Envia o link por e-mail aos fornecedores que têm e-mail cadastrado. */
  readonly sendEmail?: boolean;
}
