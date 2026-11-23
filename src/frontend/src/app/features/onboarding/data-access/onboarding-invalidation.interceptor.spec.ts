import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  OnboardingInvalidation,
  onboardingInvalidationInterceptor,
} from './onboarding-invalidation.interceptor';

describe('onboardingInvalidationInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let invalidation: OnboardingInvalidation;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([onboardingInvalidationInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    invalidation = TestBed.inject(OnboardingInvalidation);
  });

  afterEach(() => controller.verify());

  it('should invalidate after a successful write to an onboarding resource', () => {
    http.post('/api/suppliers', {}).subscribe();
    http.post('/api/shopping-lists/list-1/quotation-requests', {}).subscribe();
    http.delete('/api/purchase-orders/order-1').subscribe();

    controller.expectOne('/api/suppliers').flush({});
    controller.expectOne('/api/shopping-lists/list-1/quotation-requests').flush({});
    controller.expectOne('/api/purchase-orders/order-1').flush(null);

    expect(invalidation.revision()).toBe(3);
  });

  it('should ignore reads, failures and unrelated writes', () => {
    http.get('/api/suppliers').subscribe();
    http.post('/api/shopping-lists/list-1/suppliers/supplier-1', null).subscribe();
    http.post('/api/purchase-orders', {}).subscribe({ error: () => undefined });

    controller.expectOne('/api/suppliers').flush([]);
    controller.expectOne('/api/shopping-lists/list-1/suppliers/supplier-1').flush({});
    controller
      .expectOne('/api/purchase-orders')
      .flush({}, { status: 400, statusText: 'Bad Request' });

    expect(invalidation.revision()).toBe(0);
  });
});
