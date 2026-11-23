import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SupplierPortalService } from '../../data-access/supplier-portal.service';
import { PublicSupplierQuotationDto } from '../../dtos/supplier-portal.dto';
import { SupplierQuotationPageComponent } from './supplier-quotation-page.component';

const QUOTATION: PublicSupplierQuotationDto = {
  code: 'COT-001',
  buyerName: 'Loja Central',
  supplierName: 'Atacado Alfa',
  subject: 'Material de escritório',
  responseDeadline: '2099-12-31',
  deliveryAddress: null,
  instructions: null,
  canRespond: true,
  respondedAtUtc: null,
  terms: null,
  items: [
    { id: 'item-1', name: 'Caneta azul', quantity: 10, unit: 'un', unitPrice: null },
    { id: 'item-2', name: 'Papel A4', quantity: 5, unit: 'cx', unitPrice: null },
  ],
};

function httpError(status: number, errorCode?: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: errorCode ? { errorCode } : null });
}

describe('SupplierQuotationPageComponent', () => {
  let service: { get: ReturnType<typeof vi.fn>; respond: ReturnType<typeof vi.fn> };

  async function create(): Promise<ComponentFixture<SupplierQuotationPageComponent>> {
    await TestBed.configureTestingModule({
      imports: [SupplierQuotationPageComponent],
      providers: [
        { provide: SupplierPortalService, useValue: service },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ token: 'tok-123' }) } },
        },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(SupplierQuotationPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function type(fixture: ComponentFixture<unknown>, selector: string, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  }

  function submit(fixture: ComponentFixture<unknown>): void {
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="portal-submit"]')!
      .click();
    fixture.detectChanges();
  }

  beforeEach(() => {
    service = { get: vi.fn(() => of(QUOTATION)), respond: vi.fn(() => of(QUOTATION)) };
  });

  it.each([
    [410, 'quotation_link_revoked', 'Este link foi cancelado'],
    [410, 'quotation_link_expired', 'Este link expirou'],
    [404, 'quotation_link_not_found', 'Link não encontrado'],
  ])('mostra mensagem clara para %i %s', async (status, code, title) => {
    service.get.mockReturnValue(throwError(() => httpError(status, code)));
    const fixture = await create();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(title);
    expect(fixture.nativeElement.querySelector('[data-testid="portal-submit"]')).toBeNull();
  });

  it('mostra os itens e não envia sem nenhum preço', async () => {
    const fixture = await create();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Caneta azul');
    expect(text).toContain('Papel A4');

    submit(fixture);

    expect(service.respond).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('pelo menos um item');
  });

  it('rejeita preço negativo e não envia', async () => {
    const fixture = await create();
    type(fixture, '[data-testid="portal-price-0"]', '-5');
    submit(fixture);

    expect(service.respond).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Use um valor como 12,50.');
  });

  it('envia preços e condições e mostra a confirmação', async () => {
    const fixture = await create();
    type(fixture, '[data-testid="portal-price-0"]', '12,5');
    type(fixture, '[data-testid="portal-freight"]', '30');
    submit(fixture);

    expect(service.respond).toHaveBeenCalledTimes(1);
    const [token, body] = service.respond.mock.calls[0];
    expect(token).toBe('tok-123');
    expect(body.prices).toEqual([{ itemId: 'item-1', unitPrice: 12.5 }]);
    expect(body.freightCost).toBe(30);
    expect(fixture.nativeElement.querySelector('[data-testid="portal-sent"]')).not.toBeNull();
  });

  it('mantém os valores e avisa quando o prazo terminou durante o envio', async () => {
    service.respond.mockReturnValue(
      throwError(() => httpError(409, 'quotation_response_deadline_passed')),
    );
    const fixture = await create();
    type(fixture, '[data-testid="portal-price-0"]', '10');
    submit(fixture);

    expect(fixture.nativeElement.textContent).toContain('O prazo para responder terminou');
    expect(fixture.nativeElement.querySelector('[data-testid="portal-submit"]')).toBeNull();
  });

  it('mostra erro de rede no envio sem perder o que foi digitado', async () => {
    service.respond.mockReturnValue(throwError(() => httpError(0)));
    const fixture = await create();
    type(fixture, '[data-testid="portal-price-0"]', '10');
    submit(fixture);

    expect(fixture.nativeElement.querySelector('[data-testid="portal-submit-error"]')).not.toBeNull();
    expect(
      (fixture.nativeElement.querySelector('[data-testid="portal-price-0"]') as HTMLInputElement).value,
    ).toBe('10,00');
  });
});
