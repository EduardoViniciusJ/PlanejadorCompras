/** Mesmo teto aceito pelo servidor para preços e valores das condições comerciais. */
export const MAXIMUM_AMOUNT = 999_999_999.99;

export type MoneyParseResult =
  | { readonly kind: 'empty' }
  | { readonly kind: 'invalid' }
  | { readonly kind: 'value'; readonly value: number };

/**
 * Lê um valor em reais digitado à mão: "12,5", "12.50", "1.234,56" ou "R$ 1.234,56".
 * Rejeita negativo, texto, mais de duas casas decimais e valores acima do teto.
 */
export function parseMoney(text: string): MoneyParseResult {
  const cleaned = text.replace(/\s/g, '').replace(/^R\$/i, '');
  if (!cleaned) return { kind: 'empty' };

  let normalized: string;
  if (cleaned.includes(',')) {
    // Formato brasileiro: ponto separa milhar, vírgula separa centavos.
    if (!/^\d{1,3}(\.\d{3})*(,\d{1,2})?$|^\d+(,\d{1,2})?$/.test(cleaned)) {
      return { kind: 'invalid' };
    }
    normalized = cleaned.replace(/\./g, '').replace(',', '.');
  } else if (/^\d{1,3}(\.\d{3})+$/.test(cleaned)) {
    // "1.234" sem vírgula é milhar, não um mil e duzentos e trinta e quatro milésimos.
    normalized = cleaned.replace(/\./g, '');
  } else if (/^\d+(\.\d{1,2})?$/.test(cleaned)) {
    normalized = cleaned;
  } else {
    return { kind: 'invalid' };
  }

  const value = Number(normalized);
  if (!Number.isFinite(value) || value < 0 || value > MAXIMUM_AMOUNT) {
    return { kind: 'invalid' };
  }

  return { kind: 'value', value: Math.round(value * 100) / 100 };
}

/** Valor no formato que o fornecedor digita: "1234,50" (sem símbolo nem milhar). */
export function formatMoneyInput(value: number): string {
  return value.toFixed(2).replace('.', ',');
}
