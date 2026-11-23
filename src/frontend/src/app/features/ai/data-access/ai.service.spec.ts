import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { buildApiUrl } from '../../../core/api/api-url';
import { AiService } from './ai.service';

describe('AiService', () => {
  let service: AiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the AI status', () => {
    service.getStatus().subscribe();

    const request = http.expectOne(buildApiUrl('/api/ai/status'));
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('sends the pasted text and the file as multipart form data', () => {
    const file = new File(['%PDF-1.7'], 'cotacao.pdf', { type: 'application/pdf' });

    service.extractQuote('list-1', '  Arroz 24,90  ', file).subscribe();

    const request = http.expectOne(
      buildApiUrl('/api/shopping-lists/list-1/ai/quote-extraction'),
    );
    const body = request.request.body as FormData;
    expect(request.request.method).toBe('POST');
    expect(body).toBeInstanceOf(FormData);
    expect(body.get('text')).toBe('Arroz 24,90');
    expect((body.get('file') as File).name).toBe('cotacao.pdf');
    request.flush({ lines: [], generalConditions: null, discardedLines: 0, usage: {} });
  });

  it('omits empty text and missing file', () => {
    service.draftShoppingList('   ', null).subscribe();

    const request = http.expectOne(buildApiUrl('/api/ai/shopping-list-draft'));
    const body = request.request.body as FormData;
    expect(body.has('text')).toBe(false);
    expect(body.has('file')).toBe(false);
    request.flush({ suggestedName: '', items: [], discardedItems: 0, usage: {} });
  });

  it('saves confirmed prices through the batch endpoint', () => {
    const batch = { supplierId: 'supplier-a', quotes: [{ shoppingItemId: 'item-1', unitPrice: 24.9 }] };

    service.applyQuotes('list-1', batch).subscribe();

    const request = http.expectOne(buildApiUrl('/api/shopping-lists/list-1/quotes/batch'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(batch);
    request.flush({ created: 1, updated: 0 });
  });
});
