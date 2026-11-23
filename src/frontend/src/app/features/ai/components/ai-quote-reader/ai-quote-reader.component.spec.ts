import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AiService } from '../../data-access/ai.service';
import { AiStatusDto, QuoteExtractionResponseDto } from '../../dtos/ai.dto';
import { AiQuoteReaderComponent } from './ai-quote-reader.component';

const STATUS: AiStatusDto = {
  enabled: true,
  used: 3,
  limit: 20,
  remaining: 17,
  renewsOn: '2026-12-01',
  maxFileBytes: 5242880,
  maxInputCharacters: 20000,
};

const ITEMS = [
  { id: 'rice', name: 'Arroz tipo 1 pacote 5 kg', quantity: 20, unit: 'pct' },
  { id: 'milk', name: 'Leite integral 1 L', quantity: 120, unit: 'un' },
  { id: 'beans', name: 'Feijão carioca 1 kg', quantity: 30, unit: 'kg' },
];

const SUPPLIERS = [
  { id: 'supplier-a', name: 'Atacado Bom Preço' },
  { id: 'supplier-b', name: 'Distribuidora Central' },
];

const EXTRACTION: QuoteExtractionResponseDto = {
  lines: [
    {
      lineNumber: 1,
      supplierDescription: 'Arroz T1 5kg',
      supplierUnit: 'pct',
      supplierUnitPrice: 24.9,
      shoppingItemId: 'rice',
      unitPrice: 24.9,
      confidence: 'high',
      note: null,
    },
    {
      lineNumber: 2,
      supplierDescription: 'Leite integral cx c/ 12',
      supplierUnit: 'cx c/ 12',
      supplierUnitPrice: 59.88,
      shoppingItemId: 'milk',
      unitPrice: 4.99,
      confidence: 'medium',
      note: 'Caixa com 12: 59,88 / 12 = 4,99 por unidade.',
    },
    {
      lineNumber: 3,
      supplierDescription: 'Feijão preto 1kg',
      supplierUnit: 'kg',
      supplierUnitPrice: 8.5,
      shoppingItemId: 'beans',
      unitPrice: 8.5,
      confidence: 'low',
      note: 'A lista pede feijão carioca.',
    },
    {
      lineNumber: 4,
      supplierDescription: 'Óleo de soja 900 mL',
      supplierUnit: 'un',
      supplierUnitPrice: 7.5,
      shoppingItemId: null,
      unitPrice: 7.5,
      confidence: 'none',
      note: null,
    },
  ],
  generalConditions: 'Pagamento em 28 dias.',
  discardedLines: 1,
  usage: { ...STATUS, used: 4, remaining: 16 },
};

describe('AiQuoteReaderComponent', () => {
  let fixture: ComponentFixture<AiQuoteReaderComponent>;
  let aiService: {
    getStatus: ReturnType<typeof vi.fn>;
    extractQuote: ReturnType<typeof vi.fn>;
    applyQuotes: ReturnType<typeof vi.fn>;
  };

  async function create(status: AiStatusDto = STATUS, suppliers = SUPPLIERS): Promise<void> {
    aiService = {
      getStatus: vi.fn(() => of(status)),
      extractQuote: vi.fn(() => of(EXTRACTION)),
      applyQuotes: vi.fn(() => of({ created: 2, updated: 0 })),
    };
    await TestBed.configureTestingModule({
      imports: [AiQuoteReaderComponent],
      providers: [{ provide: AiService, useValue: aiService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AiQuoteReaderComponent);
    fixture.componentRef.setInput('listId', 'list-1');
    fixture.componentRef.setInput('items', ITEMS);
    fixture.componentRef.setInput('suppliers', suppliers);
    fixture.componentRef.setInput(
      'existingPrices',
      new Map([['rice:supplier-a', { unitPrice: 30 }]]),
    );
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const query = <T extends HTMLElement>(selector: string): T =>
    element().querySelector(selector) as T;
  const openButton = (): HTMLButtonElement => query('[data-testid="ai-read-quote"]');
  const confirmButton = (): HTMLButtonElement => query('[data-testid="ai-confirm"]');
  const row = (line: number): HTMLElement => query(`[data-testid="ai-row-${line}"]`);

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function readQuote(): Promise<void> {
    openButton().click();
    await settle();
    const textarea = query<HTMLTextAreaElement>('#ai-source-text');
    textarea.value = 'Arroz T1 5kg R$ 24,90';
    textarea.dispatchEvent(new Event('input'));
    await settle();
    query<HTMLButtonElement>('[data-testid="ai-read-submit"]').click();
    await settle();
  }

  async function chooseSupplier(id: string): Promise<void> {
    const select = query<HTMLSelectElement>('[data-testid="ai-supplier"]');
    select.value = id;
    select.dispatchEvent(new Event('change'));
    await settle();
  }

  it('disables the button and explains when AI is not configured', async () => {
    await create({ ...STATUS, enabled: false });

    expect(openButton().disabled).toBe(true);
    expect(element().textContent).toContain('A IA não está configurada neste servidor');

    openButton().click();
    await settle();
    expect(query('app-modal-dialog')).toBeNull();
  });

  it('disables the button when the monthly limit was reached, showing the renewal date', async () => {
    await create({ ...STATUS, used: 20, remaining: 0 });

    expect(openButton().disabled).toBe(true);
    expect(element().textContent).toContain(
      'Você já usou os 20 usos de IA deste mês. O limite renova em 01/12/2026.',
    );
  });

  it('shows the monthly usage and only reads when there is something to send', async () => {
    await create();
    openButton().click();
    await settle();

    expect(query('[data-testid="ai-usage"]').textContent).toContain(
      '3 de 20 usos de IA neste mês',
    );
    expect(query<HTMLButtonElement>('[data-testid="ai-read-submit"]').disabled).toBe(true);
    expect(aiService.extractQuote).not.toHaveBeenCalled();
  });

  it('opens a review and saves nothing until the buyer confirms', async () => {
    await create();
    await readQuote();

    expect(aiService.extractQuote).toHaveBeenCalledWith('list-1', 'Arroz T1 5kg R$ 24,90', null);
    expect(aiService.applyQuotes).not.toHaveBeenCalled();
    expect(element().textContent).toContain('Nenhum preço foi gravado ainda');
    expect(element().textContent).toContain('Pagamento em 28 dias.');
    expect(element().textContent).toContain('1 linha foi ignorada');

    // Confidence is visible on every line.
    expect(row(1).textContent).toContain('Alta');
    expect(row(2).textContent).toContain('Média');
    expect(row(2).textContent).toContain('Caixa com 12');
    expect(row(3).textContent).toContain('Baixa');
    expect(row(4).textContent).toContain('Sem correspondência');

    // Low-confidence and unmatched lines are not selected by default.
    const checked = (line: number) =>
      (row(line).querySelector('input[type="checkbox"]') as HTMLInputElement).checked;
    expect([checked(1), checked(2), checked(3), checked(4)]).toEqual([true, true, false, false]);
  });

  it('requires the buyer to choose the supplier before saving', async () => {
    await create();
    await readQuote();

    expect(confirmButton().disabled).toBe(true);

    await chooseSupplier('supplier-a');

    expect(confirmButton().disabled).toBe(false);
    expect(confirmButton().textContent).toContain('Gravar 2 preços');
    expect(row(1).textContent).toContain('Substitui o preço atual de');
  });

  it('saves only the confirmed lines, with the corrected values, for the chosen supplier', async () => {
    await create();
    const applied = vi.fn();
    fixture.componentInstance.applied.subscribe(applied);
    await readQuote();
    await chooseSupplier('supplier-b');

    // Correct a price, discard a line and accept the low-confidence one.
    const price = row(1).querySelector('input[type="text"]') as HTMLInputElement;
    price.value = '23,50';
    price.dispatchEvent(new Event('input'));
    const discard = row(2).querySelector('input[type="checkbox"]') as HTMLInputElement;
    discard.checked = false;
    discard.dispatchEvent(new Event('change'));
    const accept = row(3).querySelector('input[type="checkbox"]') as HTMLInputElement;
    accept.checked = true;
    accept.dispatchEvent(new Event('change'));
    await settle();

    confirmButton().click();
    await settle();

    expect(aiService.applyQuotes).toHaveBeenCalledWith('list-1', {
      supplierId: 'supplier-b',
      quotes: [
        { shoppingItemId: 'rice', unitPrice: 23.5 },
        { shoppingItemId: 'beans', unitPrice: 8.5 },
      ],
    });
    expect(applied).toHaveBeenCalledWith({
      supplierName: 'Distribuidora Central',
      created: 2,
      updated: 0,
    });
    expect(query('app-modal-dialog')).toBeNull();
  });

  it('blocks saving when a price is invalid or two lines use the same item', async () => {
    await create(STATUS, [SUPPLIERS[0]]);
    await readQuote();

    // A single supplier is preselected.
    expect(confirmButton().disabled).toBe(false);

    const price = row(1).querySelector('input[type="text"]') as HTMLInputElement;
    price.value = '0';
    price.dispatchEvent(new Event('input'));
    await settle();
    expect(confirmButton().disabled).toBe(true);
    expect(row(1).textContent).toContain('Informe um preço maior que zero.');

    price.value = '24,90';
    price.dispatchEvent(new Event('input'));
    const item = row(4).querySelector('select') as HTMLSelectElement;
    item.value = 'rice';
    item.dispatchEvent(new Event('change'));
    await settle();

    expect(confirmButton().disabled).toBe(true);
    expect(row(4).textContent).toContain('Este item está em mais de uma linha.');
    expect(aiService.applyQuotes).not.toHaveBeenCalled();
  });

  it('shows the server message when the reading fails and keeps the dialog open', async () => {
    await create();
    aiService.extractQuote.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 429,
            error: {
              title: 'Você já usou os 20 usos de IA deste mês. O limite renova em 01/12/2026.',
              errorCode: 'ai_usage_limit_exceeded',
            },
          }),
      ),
    );

    await readQuote();

    expect(query('[data-testid="ai-error"]').textContent).toContain('O limite renova em 01/12/2026');
    expect(query('#ai-source-text')).not.toBeNull();
    expect(aiService.getStatus).toHaveBeenCalledTimes(2);
  });

  it('tells the buyer when no price was found', async () => {
    await create();
    aiService.extractQuote.mockReturnValue(
      of({ ...EXTRACTION, lines: [], generalConditions: null, discardedLines: 0 }),
    );

    await readQuote();

    expect(query('[data-testid="ai-empty"]').textContent).toContain('não encontrou preços');
    expect(confirmButton()).toBeNull();
  });

  it('does not read while the list has no supplier', async () => {
    await create(STATUS, []);
    openButton().click();
    await settle();
    const textarea = query<HTMLTextAreaElement>('#ai-source-text');
    textarea.value = 'Arroz 10,00';
    textarea.dispatchEvent(new Event('input'));
    await settle();

    expect(element().textContent).toContain('Adicione o fornecedor a esta lista');
    expect(query<HTMLButtonElement>('[data-testid="ai-read-submit"]').disabled).toBe(true);
  });
});
