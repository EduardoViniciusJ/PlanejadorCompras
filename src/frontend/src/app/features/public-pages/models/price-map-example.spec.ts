import { buildPriceMapExample, formatBrl } from './price-map-example';

describe('buildPriceMapExample', () => {
  const view = buildPriceMapExample({
    listName: 'Lista de teste',
    suppliers: ['A', 'B'],
    rows: [
      { item: 'Item 1', quantity: 3, unit: 'kg', prices: [10.1, 9.9] },
      { item: 'Item 2', quantity: 10, unit: 'un', prices: [2, 2] },
      { item: 'Item 3', quantity: 100, unit: 'cx', prices: [12.5, 13] },
    ],
  });

  it('should mark the lowest price of each row, including ties', () => {
    expect(view.rows.map((row) => row.cells.map((cell) => cell.best))).toEqual([
      [false, true],
      [true, true],
      [true, false],
    ]);
  });

  it('should total each supplier and the best combination in Brazilian format', () => {
    // A: 30,30 + 20,00 + 1.250,00 | B: 29,70 + 20,00 + 1.300,00 | melhor: 29,70 + 20,00 + 1.250,00
    expect(view.totals).toEqual(['1.300,30', '1.349,70']);
    expect(view.bestCombination).toBe('1.299,70');
    expect(view.rows[0].quantity).toBe('3 kg');
  });

  it('should format values with two decimals', () => {
    expect(formatBrl(7.9)).toBe('7,90');
    expect(formatBrl(1234.5)).toBe('1.234,50');
  });
});
