import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SupplierService } from '../../data-access/supplier.service';
import { SuppliersPageComponent } from './suppliers-page.component';

describe('SuppliersPageComponent', () => {
  let fixture: ComponentFixture<SuppliersPageComponent>;
  const supplier = {
    id: 'supplier-1',
    name: 'Papelaria Central',
    cnpj: null,
    address: null,
    contact: null,
    createdAt: new Date(),
  };
  const service = {
    getAll: vi.fn(() => of([supplier])),
    create: vi.fn(() => of(supplier)),
    update: vi.fn(() => of(supplier)),
    delete: vi.fn((): Observable<void> => of(undefined)),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [SuppliersPageComponent],
      providers: [{ provide: SupplierService, useValue: service }],
    }).compileComponents();
    fixture = TestBed.createComponent(SuppliersPageComponent);
    fixture.detectChanges();
  });

  it('should render and filter suppliers', () => {
    expect(host().textContent).toContain('Papelaria Central');
    const emptyValues = Array.from(host().querySelectorAll<HTMLElement>('.empty-value'));
    expect(emptyValues).toHaveLength(3);
    expect(emptyValues.every((element) => element.textContent?.trim() === 'Não informado')).toBe(
      true,
    );
    const search = host().querySelector<HTMLInputElement>('input[type="search"]')!;
    search.value = 'inexistente';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(host().textContent).toContain('Nenhum fornecedor encontrado');
  });

  it('should create a supplier', () => {
    click('[data-testid="add-supplier"]');
    const input = host().querySelector<HTMLInputElement>('input[formControlName="name"]')!;
    input.value = 'Novo fornecedor';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    host().querySelector<HTMLFormElement>('.supplier-form')!.dispatchEvent(new Event('submit'));
    expect(service.create).toHaveBeenCalledWith({
      name: 'Novo fornecedor',
      cnpj: null,
      address: null,
      contact: null,
    });
  });

  it('should delete a supplier after confirmation', () => {
    click('[data-testid="delete-supplier"]');
    click('[data-testid="confirm-delete-supplier"]');
    expect(service.delete).toHaveBeenCalledWith('supplier-1');
  });

  it('should block the confirmation only when the supplier is in use', () => {
    service.delete.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 409 })));
    click('[data-testid="delete-supplier"]');
    click('[data-testid="confirm-delete-supplier"]');

    const confirm = confirmButton();
    expect(host().querySelector('[role="alert"]')?.textContent).toContain(
      'Este fornecedor possui preços cadastrados',
    );
    expect(confirm.disabled).toBe(true);
    expect(confirm.textContent).toContain('Fornecedor em uso');
  });

  it('should allow retrying the deletion after a non-conflict failure', () => {
    service.delete.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })));
    click('[data-testid="delete-supplier"]');
    click('[data-testid="confirm-delete-supplier"]');

    expect(host().querySelector('[role="alert"]')?.textContent).toContain(
      'Não foi possível excluir o fornecedor agora.',
    );
    expect(confirmButton().disabled).toBe(false);
    expect(confirmButton().textContent).toContain('Excluir fornecedor');
    expect(confirmButton().textContent).not.toContain('Fornecedor em uso');

    click('[data-testid="confirm-delete-supplier"]');

    expect(service.delete).toHaveBeenCalledTimes(2);
    expect(host().querySelector('[data-testid="confirm-delete-supplier"]')).toBeNull();
    expect(host().textContent).toContain('Fornecedor excluído com sucesso.');
  });

  it('should paginate the table in pages of ten suppliers', () => {
    service.getAll.mockReturnValueOnce(
      of(
        Array.from({ length: 12 }, (_, index) => ({
          ...supplier,
          id: `supplier-${index + 1}`,
          name: `Fornecedor ${index + 1}`,
        })),
      ),
    );
    fixture = TestBed.createComponent(SuppliersPageComponent);
    fixture.detectChanges();

    expect(host().querySelectorAll('tbody tr')).toHaveLength(10);
    click('[data-testid="suppliers-next-page"]');
    expect(host().querySelectorAll('tbody tr')).toHaveLength(2);
    expect(host().textContent).toContain('Página 2 de 2');
  });

  function confirmButton(): HTMLButtonElement {
    return host().querySelector<HTMLButtonElement>('[data-testid="confirm-delete-supplier"]')!;
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function click(selector: string): void {
    host().querySelector<HTMLElement>(selector)!.click();
    fixture.detectChanges();
  }
});
