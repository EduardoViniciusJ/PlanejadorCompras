import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { CurrentUser } from '../../../../core/auth/models/current-user.model';
import { PurchaseOrderService } from '../../../purchase-orders/data-access/purchase-order.service';
import { QuotationRequestService } from '../../../quotation-requests/data-access/quotation-request.service';
import { ShoppingList } from '../../../shopping-lists/models/shopping-list.model';
import { SupplierService } from '../../../suppliers/data-access/supplier.service';
import { OnboardingService } from '../../data-access/onboarding.service';
import { OnboardingChecklistComponent } from './onboarding-checklist.component';

const STORAGE_KEY = 'planejador:onboarding-checklist:user-1';

const LIST_WITH_PRICES: ShoppingList = {
  id: 'list-1',
  name: 'Hortifrúti da semana',
  description: null,
  createdAt: new Date('2026-11-20T12:00:00Z'),
  itemCount: 4,
  quotedItemCount: 2,
  estimatedTotal: 180,
  status: 'awaiting-quotes',
};

const EMPTY_LIST: ShoppingList = {
  ...LIST_WITH_PRICES,
  id: 'list-2',
  name: 'Limpeza',
  itemCount: 0,
  quotedItemCount: 0,
  estimatedTotal: 0,
  status: 'draft',
};

describe('OnboardingChecklistComponent', () => {
  let fixture: ComponentFixture<OnboardingChecklistComponent>;
  const currentUser = signal<CurrentUser | null>({
    id: 'user-1',
    name: 'Ana',
    email: 'ana@example.com',
  });
  const supplierService = { getAll: vi.fn() };
  const quotationRequestService = { getAll: vi.fn() };
  const purchaseOrderService = { getAll: vi.fn() };

  beforeEach(async () => {
    localStorage.clear();
    currentUser.set({ id: 'user-1', name: 'Ana', email: 'ana@example.com' });
    supplierService.getAll.mockReset().mockReturnValue(of([]));
    quotationRequestService.getAll.mockReset().mockReturnValue(of([]));
    purchaseOrderService.getAll.mockReset().mockReturnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [OnboardingChecklistComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: currentUser.asReadonly() } },
        { provide: SupplierService, useValue: supplierService },
        { provide: QuotationRequestService, useValue: quotationRequestService },
        { provide: PurchaseOrderService, useValue: purchaseOrderService },
      ],
    }).compileComponents();
  });

  afterEach(() => localStorage.clear());

  it('should list the five steps as pending when nothing was done', async () => {
    await render([]);

    expect(checklist()).toBeTruthy();
    expect(text('[data-testid="onboarding-progress"]')).toBe('0 de 5 concluídos');
    expect(doneSteps()).toEqual([]);
    expect(host().querySelectorAll('.onboarding-step')).toHaveLength(5);
    expect(text('[data-testid="onboarding-step-send-quotation-request"] .btn')).toBe(
      'Criar lista primeiro',
    );

    const createList = vi.fn();
    fixture.componentInstance.createListRequested.subscribe(createList);
    const firstAction = host().querySelector<HTMLButtonElement>(
      '[data-testid="onboarding-step-create-list"] button',
    )!;
    expect(firstAction.classList).toContain('btn-primary');
    firstAction.click();

    expect(createList).toHaveBeenCalledTimes(1);
  });

  it('should derive partial progress from lists and API data', async () => {
    supplierService.getAll.mockReturnValue(of([{ id: 'supplier-1' }]));

    await render([LIST_WITH_PRICES, EMPTY_LIST]);

    expect(text('[data-testid="onboarding-progress"]')).toBe('3 de 5 concluídos');
    expect(doneSteps()).toEqual(['create-list', 'add-supplier', 'enter-prices']);

    const nextStep = host().querySelector('.onboarding-step-next');
    expect(nextStep?.getAttribute('data-testid')).toBe('onboarding-step-send-quotation-request');

    const requested = vi.fn();
    fixture.componentInstance.quotationRequested.subscribe(requested);
    host()
      .querySelector<HTMLButtonElement>(
        '[data-testid="onboarding-step-send-quotation-request"] button',
      )!
      .click();
    expect(requested).toHaveBeenCalledWith(LIST_WITH_PRICES);

    const orderLink = host().querySelector<HTMLAnchorElement>(
      '[data-testid="onboarding-step-issue-purchase-order"] a',
    )!;
    expect(orderLink.getAttribute('href')).toBe('/app/lists/list-1/equalization');
  });

  it('should hide itself and remember when every step is done', async () => {
    supplierService.getAll.mockReturnValue(of([{ id: 'supplier-1' }]));
    quotationRequestService.getAll.mockReturnValue(of([{ id: 'request-1' }]));
    purchaseOrderService.getAll.mockReturnValue(of([{ id: 'order-1' }]));

    await render([LIST_WITH_PRICES]);

    expect(checklist()).toBeNull();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('completed');
  });

  it('should hide when dismissed and keep the choice per user', async () => {
    await render([]);

    host().querySelector<HTMLButtonElement>('[data-testid="onboarding-dismiss"]')!.click();
    fixture.detectChanges();

    expect(checklist()).toBeNull();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dismissed');

    currentUser.set({ id: 'user-2', name: 'Bruno', email: 'bruno@example.com' });
    await settle();

    expect(checklist()).toBeTruthy();
  });

  it('should not call the API when it was already dismissed', async () => {
    localStorage.setItem(STORAGE_KEY, 'dismissed');

    await render([]);

    expect(checklist()).toBeNull();
    expect(supplierService.getAll).not.toHaveBeenCalled();
  });

  it('should stay hidden when loading the progress fails', async () => {
    purchaseOrderService.getAll.mockReturnValue(throwError(() => new Error('offline')));

    await render([]);

    expect(checklist()).toBeNull();
    expect(host().querySelector('[role="status"]')).toBeNull();
  });

  it('should reuse the loaded progress and reload only after it is invalidated', async () => {
    await render([]);
    fixture.destroy();

    await render([]);
    expect(supplierService.getAll).toHaveBeenCalledTimes(1);

    supplierService.getAll.mockReturnValue(of([{ id: 'supplier-1' }]));
    TestBed.inject(OnboardingService).invalidate();
    await settle();

    expect(supplierService.getAll).toHaveBeenCalledTimes(2);
    expect(doneSteps()).toEqual(['add-supplier']);
  });

  async function render(lists: readonly ShoppingList[]): Promise<void> {
    fixture = TestBed.createComponent(OnboardingChecklistComponent);
    fixture.componentRef.setInput('lists', lists);
    await settle();
  }

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function checklist(): HTMLElement | null {
    return host().querySelector('[data-testid="onboarding-checklist"]');
  }

  function text(selector: string): string {
    return host().querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
  }

  function doneSteps(): string[] {
    return Array.from(host().querySelectorAll('.onboarding-step-done')).map(
      (element) => element.getAttribute('data-testid')?.replace('onboarding-step-', '') ?? '',
    );
  }
});
