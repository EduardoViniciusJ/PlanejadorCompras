import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ShoppingItemService } from '../../../shopping-items/data-access/shopping-item.service';
import { ShoppingListService } from '../../../shopping-lists/data-access/shopping-list.service';
import { AiService } from '../../data-access/ai.service';
import { AiStatusDto } from '../../dtos/ai.dto';
import { AiListCreatorComponent } from './ai-list-creator.component';

const STATUS: AiStatusDto = {
  enabled: true,
  used: 0,
  limit: 20,
  remaining: 20,
  renewsOn: '2026-12-01',
  maxFileBytes: 5242880,
  maxInputCharacters: 20000,
};

describe('AiListCreatorComponent', () => {
  let fixture: ComponentFixture<AiListCreatorComponent>;
  let aiService: { getStatus: ReturnType<typeof vi.fn>; draftShoppingList: ReturnType<typeof vi.fn> };
  let listService: { createWithId: ReturnType<typeof vi.fn> };
  let itemService: { createBatch: ReturnType<typeof vi.fn> };

  async function create(status: AiStatusDto = STATUS): Promise<void> {
    aiService = {
      getStatus: vi.fn(() => of(status)),
      draftShoppingList: vi.fn(() =>
        of({
          suggestedName: 'Reposição de mercearia',
          items: [
            { name: 'Arroz', quantity: 20, unit: 'kg' },
            { name: 'Leite integral', quantity: 10.5, unit: 'cx' },
          ],
          discardedItems: 0,
          usage: { ...status, used: 1, remaining: 19 },
        }),
      ),
    };
    listService = { createWithId: vi.fn(() => of('new-list')) };
    itemService = { createBatch: vi.fn(() => of(['item-1', 'item-2'])) };

    await TestBed.configureTestingModule({
      imports: [AiListCreatorComponent],
      providers: [
        { provide: AiService, useValue: aiService },
        { provide: ShoppingListService, useValue: listService },
        { provide: ShoppingItemService, useValue: itemService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AiListCreatorComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const query = <T extends HTMLElement>(selector: string): T =>
    element().querySelector(selector) as T;
  const openButton = (): HTMLButtonElement => query('[data-testid="ai-create-list"]');
  const submitButton = (): HTMLButtonElement =>
    Array.from(element().querySelectorAll('button')).find((button) =>
      button.textContent?.includes('Criar lista com estes itens'),
    ) as HTMLButtonElement;

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function propose(): Promise<void> {
    openButton().click();
    await settle();
    const textarea = query<HTMLTextAreaElement>('#ai-source-text');
    textarea.value = 'preciso de 20 kg de arroz, 10 caixas de leite';
    textarea.dispatchEvent(new Event('input'));
    await settle();
    query<HTMLButtonElement>('[data-testid="ai-propose-submit"]').click();
    await settle();
  }

  it('disables the button and explains when AI is not configured', async () => {
    await create({ ...STATUS, enabled: false });

    expect(openButton().disabled).toBe(true);
    expect(element().textContent).toContain('A IA não está configurada neste servidor');
  });

  it('shows the proposal for review without creating anything', async () => {
    await create();
    await propose();

    expect(aiService.draftShoppingList).toHaveBeenCalledWith(
      'preciso de 20 kg de arroz, 10 caixas de leite',
      null,
    );
    expect(query<HTMLInputElement>('[data-testid="ai-list-name"]').value).toBe(
      'Reposição de mercearia',
    );
    const names = Array.from(element().querySelectorAll<HTMLInputElement>('[data-name]')).map(
      (input) => input.value,
    );
    expect(names).toEqual(['Arroz', 'Leite integral']);
    expect(element().textContent).toContain('Nada foi criado ainda');
    expect(listService.createWithId).not.toHaveBeenCalled();
    expect(itemService.createBatch).not.toHaveBeenCalled();
  });

  it('creates the list and its items only after the user confirms', async () => {
    await create();
    const created = vi.fn();
    fixture.componentInstance.created.subscribe(created);
    await propose();

    const name = query<HTMLInputElement>('[data-testid="ai-list-name"]');
    name.value = 'Compras da semana';
    name.dispatchEvent(new Event('input'));
    await settle();
    submitButton().click();
    await settle();

    expect(listService.createWithId).toHaveBeenCalledWith({
      name: 'Compras da semana',
      description: null,
    });
    expect(itemService.createBatch).toHaveBeenCalledWith([
      { shoppingListId: 'new-list', name: 'Arroz', quantity: 20, unit: 'kg' },
      { shoppingListId: 'new-list', name: 'Leite integral', quantity: 10.5, unit: 'cx' },
    ]);
    expect(created).toHaveBeenCalledWith({
      listId: 'new-list',
      name: 'Compras da semana',
      itemCount: 2,
    });
  });

  it('does not create the list twice when adding the items fails and the user retries', async () => {
    await create();
    itemService.createBatch.mockReturnValueOnce(throwError(() => new Error('fail')));
    await propose();

    submitButton().click();
    await settle();
    expect(element().textContent).toContain('A lista foi criada, mas não foi possível adicionar os itens');

    submitButton().click();
    await settle();

    expect(listService.createWithId).toHaveBeenCalledTimes(1);
    expect(itemService.createBatch).toHaveBeenCalledTimes(2);
  });

  it('requires a list name', async () => {
    await create();
    await propose();

    const name = query<HTMLInputElement>('[data-testid="ai-list-name"]');
    name.value = '   ';
    name.dispatchEvent(new Event('input'));
    await settle();
    submitButton().click();
    await settle();

    expect(element().textContent).toContain('Informe um nome para a lista.');
    expect(listService.createWithId).not.toHaveBeenCalled();
  });

  it('keeps the input open when the AI finds no items', async () => {
    await create();
    aiService.draftShoppingList.mockReturnValue(
      of({ suggestedName: 'Lista de compras', items: [], discardedItems: 0, usage: STATUS }),
    );

    await propose();

    expect(query('[data-testid="ai-error"]').textContent).toContain('não encontrou itens');
    expect(query('#ai-source-text')).not.toBeNull();
  });
});
