import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { buildApiUrl } from '../../../core/api/api-url';
import { SupplierService } from './supplier.service';

describe('SupplierService', () => {
  let service: SupplierService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SupplierService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('should get and map suppliers across every page', () => {
    let result: readonly { createdAt: Date }[] = [];
    service.getAll().subscribe((suppliers) => (result = suppliers));

    const supplier = { id: 'supplier-1', name: 'Central', createdAt: '2026-07-11T12:00:00Z' };
    const first = httpTesting.expectOne(
      (candidate) =>
        candidate.url === buildApiUrl('/api/suppliers') &&
        candidate.params.get('page') === '1' &&
        candidate.params.get('pageSize') === '100',
    );
    expect(first.request.method).toBe('GET');
    first.flush({ items: [supplier], page: 1, pageSize: 100, totalCount: 2, totalPages: 2 });

    const second = httpTesting.expectOne(
      (candidate) =>
        candidate.url === buildApiUrl('/api/suppliers') && candidate.params.get('page') === '2',
    );
    second.flush({
      items: [{ ...supplier, id: 'supplier-2' }],
      page: 2,
      pageSize: 100,
      totalCount: 2,
      totalPages: 2,
    });

    expect(result.length).toBe(2);
    expect(result[0].createdAt).toBeInstanceOf(Date);
  });

  it('should call supplier mutation endpoints', () => {
    const payload = { name: 'Central' };
    const response = { id: 'supplier-1', name: 'Central', createdAt: '2026-07-11T12:00:00Z' };

    service.create(payload).subscribe();
    const createRequest = httpTesting.expectOne(buildApiUrl('/api/suppliers'));
    expect(createRequest.request.body).toEqual(payload);
    createRequest.flush(response);

    service.update('supplier-1', payload).subscribe();
    const updateRequest = httpTesting.expectOne(buildApiUrl('/api/suppliers/supplier-1'));
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush(response);

    service.delete('supplier-1').subscribe();
    const deleteRequest = httpTesting.expectOne(buildApiUrl('/api/suppliers/supplier-1'));
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);
  });

  it('should manage suppliers assigned to a shopping list', () => {
    const response = { id: 'supplier-1', name: 'Central', createdAt: '2026-07-11T12:00:00Z' };

    service
      .getForShoppingList('list-1')
      .subscribe((suppliers) => expect(suppliers[0].createdAt).toBeInstanceOf(Date));
    const getRequest = httpTesting.expectOne(buildApiUrl('/api/shopping-lists/list-1/suppliers'));
    expect(getRequest.request.method).toBe('GET');
    getRequest.flush([response]);

    service.addToShoppingList('list-1', 'supplier-1').subscribe();
    const addRequest = httpTesting.expectOne(
      buildApiUrl('/api/shopping-lists/list-1/suppliers/supplier-1'),
    );
    expect(addRequest.request.method).toBe('POST');
    expect(addRequest.request.body).toBeNull();
    addRequest.flush(response);

    service.removeFromShoppingList('list-1', 'supplier-1').subscribe();
    const removeRequest = httpTesting.expectOne(
      buildApiUrl('/api/shopping-lists/list-1/suppliers/supplier-1'),
    );
    expect(removeRequest.request.method).toBe('DELETE');
    removeRequest.flush(null);
  });
});
