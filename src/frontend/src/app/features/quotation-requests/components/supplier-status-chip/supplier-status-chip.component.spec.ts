import { TestBed } from '@angular/core/testing';

import { SupplierQuotationStatus } from '../../dtos/quotation-request.dto';
import { SupplierStatusChipComponent } from './supplier-status-chip.component';

describe('SupplierStatusChipComponent', () => {
  it.each<[SupplierQuotationStatus, string]>([
    ['sent', 'Enviada'],
    ['viewed', 'Vista'],
    ['responded', 'Respondida'],
  ])('mostra o status %s como "%s"', (status, label) => {
    const fixture = TestBed.createComponent(SupplierStatusChipComponent);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();

    const chip = (fixture.nativeElement as HTMLElement).querySelector('.status-chip')!;
    expect(chip.textContent).toContain(label);
    expect(chip.getAttribute('data-status')).toBe(status);
  });
});
