import { PriceMapExample } from './public-page.model';

export interface PriceMapExampleCell {
  readonly value: string;
  readonly best: boolean;
}

export interface PriceMapExampleRow {
  readonly item: string;
  readonly quantity: string;
  readonly cells: readonly PriceMapExampleCell[];
}

export interface PriceMapExampleView {
  readonly listName: string;
  readonly suppliers: readonly string[];
  readonly rows: readonly PriceMapExampleRow[];
  /** Total da compra inteira com cada fornecedor. */
  readonly totals: readonly string[];
  /** Total comprando cada item de quem cobrou menos. */
  readonly bestCombination: string;
}

const BRL = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Valor em reais no formato brasileiro, sem o símbolo (ex.: 1.234,50). */
export const formatBrl = (value: number): string => BRL.format(value);

/** Arredonda para centavos, para a soma não carregar erro de ponto flutuante. */
const cents = (value: number): number => Math.round(value * 100) / 100;

/**
 * Prepara o mapa de exemplo para exibir: marca o menor preço de cada linha e calcula os
 * totais, para que o que aparece na tela sempre feche com os preços.
 */
export function buildPriceMapExample(example: PriceMapExample): PriceMapExampleView {
  const totals = example.suppliers.map(() => 0);
  let bestCombination = 0;

  const rows = example.rows.map((row) => {
    const lowest = Math.min(...row.prices);
    bestCombination += cents(lowest * row.quantity);
    row.prices.forEach((price, index) => (totals[index] += cents(price * row.quantity)));

    return {
      item: row.item,
      quantity: `${row.quantity} ${row.unit}`,
      cells: row.prices.map((price) => ({ value: formatBrl(price), best: price === lowest })),
    };
  });

  return {
    listName: example.listName,
    suppliers: example.suppliers,
    rows,
    totals: totals.map((total) => formatBrl(cents(total))),
    bestCombination: formatBrl(cents(bestCombination)),
  };
}
