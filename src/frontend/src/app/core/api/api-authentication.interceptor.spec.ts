import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { apiAuthenticationInterceptor } from './api-authentication.interceptor';
import { apiCredentialsInterceptor } from './api-credentials.interceptor';
import { buildApiUrl } from './api-url';

const LISTS_URL = buildApiUrl('/api/shopping-lists');
const SUPPLIERS_URL = buildApiUrl('/api/suppliers');
const ORDERS_URL = buildApiUrl('/api/purchase-orders');
const REFRESH_URL = buildApiUrl('/api/auth/refresh');
const ME_URL = buildApiUrl('/api/auth/me');
const LOGIN_URL = buildApiUrl('/api/auth/login');

const UNAUTHORIZED = { status: 401, statusText: 'Unauthorized' };

describe('apiAuthenticationInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiCredentialsInterceptor, apiAuthenticationInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    httpTesting.verify();
    vi.useRealTimers();
  });

  it('renova a sessão uma vez e repete a requisição quando a API responde 401', () => {
    let result: unknown;
    http.post(LISTS_URL, { name: 'Lista' }).subscribe((response) => (result = response));

    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);

    const refresh = httpTesting.expectOne(REFRESH_URL);
    expect(refresh.request.method).toBe('POST');
    expect(refresh.request.withCredentials).toBe(true);
    expect(refresh.request.headers.get('X-Requested-With')).toBe('XmlHttpRequest');
    refresh.flush(null, { status: 204, statusText: 'No Content' });

    const retry = httpTesting.expectOne(LISTS_URL);
    expect(retry.request.body).toEqual({ name: 'Lista' });
    expect(retry.request.withCredentials).toBe(true);
    expect(retry.request.headers.get('X-Requested-With')).toBe('XmlHttpRequest');
    retry.flush({ id: 'list-1' });

    expect(result).toEqual({ id: 'list-1' });
    expect(navigate).not.toHaveBeenCalled();
  });

  it('faz as requisições concorrentes esperarem a mesma renovação', () => {
    const results: unknown[] = [];
    http.get(LISTS_URL).subscribe((response) => results.push(response));
    http.get(SUPPLIERS_URL).subscribe((response) => results.push(response));
    http.get(ORDERS_URL).subscribe((response) => results.push(response));

    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(SUPPLIERS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(ORDERS_URL).flush(null, UNAUTHORIZED);

    // Três 401, uma renovação só.
    const refresh = httpTesting.expectOne(REFRESH_URL);
    httpTesting.expectNone(LISTS_URL);
    refresh.flush(null, { status: 204, statusText: 'No Content' });

    httpTesting.expectOne(LISTS_URL).flush('lists');
    httpTesting.expectOne(SUPPLIERS_URL).flush('suppliers');
    httpTesting.expectOne(ORDERS_URL).flush('orders');

    expect(results).toEqual(['lists', 'suppliers', 'orders']);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('não entra em laço: se a requisição repetida também volta 401, vai para o login', () => {
    let error: HttpErrorResponse | undefined;
    http.get(LISTS_URL).subscribe({ error: (response: HttpErrorResponse) => (error = response) });

    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, { status: 204, statusText: 'No Content' });
    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);

    // Nenhuma segunda renovação nem terceira tentativa (o verify do afterEach confirma).
    httpTesting.expectNone(REFRESH_URL);
    httpTesting.expectNone(LISTS_URL);
    expect(error?.status).toBe(401);
    expect(navigate).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/app', reason: 'session-expired' },
    });
  });

  it('vai para o login sem repetir a requisição quando a renovação é recusada', () => {
    const errors: number[] = [];
    http.get(LISTS_URL).subscribe({ error: (response: HttpErrorResponse) => errors.push(response.status) });
    http.get(SUPPLIERS_URL).subscribe({ error: (response: HttpErrorResponse) => errors.push(response.status) });

    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(SUPPLIERS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, UNAUTHORIZED);

    httpTesting.expectNone(LISTS_URL);
    httpTesting.expectNone(SUPPLIERS_URL);
    expect(errors).toEqual([401, 401]);
    expect(navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/app', reason: 'session-expired' },
    });
  });

  it('não desloga quando a renovação falha por erro de rede ou do servidor', () => {
    let error: HttpErrorResponse | undefined;
    http.get(LISTS_URL).subscribe({ error: (response: HttpErrorResponse) => (error = response) });

    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, { status: 503, statusText: 'Service Unavailable' });

    expect(error?.status).toBe(401);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('faz uma nova renovação depois que a anterior terminou', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-23T12:00:00Z'));

    http.get(LISTS_URL).subscribe();
    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, { status: 204, statusText: 'No Content' });
    httpTesting.expectOne(LISTS_URL).flush('lists');

    // Quinze minutos depois o novo token de acesso também expira.
    vi.setSystemTime(new Date('2026-11-23T12:15:00Z'));
    http.get(SUPPLIERS_URL).subscribe();
    httpTesting.expectOne(SUPPLIERS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, { status: 204, statusText: 'No Content' });
    httpTesting.expectOne(SUPPLIERS_URL).flush('suppliers');
  });

  it('só repete, sem renovar de novo, a requisição enviada antes de uma renovação já concluída', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-23T12:00:00.000Z'));

    const results: unknown[] = [];
    http.get(LISTS_URL).subscribe((response) => results.push(response));
    http.get(SUPPLIERS_URL).subscribe((response) => results.push(response));
    const slowRequest = httpTesting.expectOne(SUPPLIERS_URL);

    vi.setSystemTime(new Date('2026-11-23T12:00:01.000Z'));
    httpTesting.expectOne(LISTS_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, { status: 204, statusText: 'No Content' });
    httpTesting.expectOne(LISTS_URL).flush('lists');

    // O 401 desta requisição chega atrasado: ela saiu com o token antigo.
    vi.setSystemTime(new Date('2026-11-23T12:00:02.000Z'));
    slowRequest.flush(null, UNAUTHORIZED);

    httpTesting.expectNone(REFRESH_URL);
    httpTesting.expectOne(SUPPLIERS_URL).flush('suppliers');
    expect(results).toEqual(['lists', 'suppliers']);
  });

  it('renova a sessão para /api/auth/me, mas deixa o redirecionamento para quem chamou', () => {
    let error: HttpErrorResponse | undefined;
    http.get(ME_URL).subscribe({ error: (response: HttpErrorResponse) => (error = response) });

    httpTesting.expectOne(ME_URL).flush(null, UNAUTHORIZED);
    httpTesting.expectOne(REFRESH_URL).flush(null, UNAUTHORIZED);

    expect(error?.status).toBe(401);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('não tenta renovar quando o 401 vem do próprio login', () => {
    let error: HttpErrorResponse | undefined;
    http
      .post(LOGIN_URL, { email: 'a@b.com', password: 'errada' })
      .subscribe({ error: (response: HttpErrorResponse) => (error = response) });

    httpTesting.expectOne(LOGIN_URL).flush(null, UNAUTHORIZED);

    httpTesting.expectNone(REFRESH_URL);
    expect(error?.status).toBe(401);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('não interfere em erros que não são 401 nem em URLs fora da API', () => {
    const statuses: number[] = [];
    http.get(LISTS_URL).subscribe({ error: (response: HttpErrorResponse) => statuses.push(response.status) });
    http
      .get('https://example.com/outro')
      .subscribe({ error: (response: HttpErrorResponse) => statuses.push(response.status) });

    httpTesting.expectOne(LISTS_URL).flush(null, { status: 403, statusText: 'Forbidden' });
    httpTesting.expectOne('https://example.com/outro').flush(null, UNAUTHORIZED);

    httpTesting.expectNone(REFRESH_URL);
    expect(statuses).toEqual([403, 401]);
    expect(navigate).not.toHaveBeenCalled();
  });
});
