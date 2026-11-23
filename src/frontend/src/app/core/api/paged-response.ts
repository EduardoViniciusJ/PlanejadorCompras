import { HttpClient, HttpParams } from '@angular/common/http';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';

import { buildApiUrl } from './api-url';

export interface PagedResponseDto<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** Maior página que a API aceita. */
export const MAX_API_PAGE_SIZE = 100;

/**
 * Lê todas as páginas de uma listagem, em requisições de até 100 itens.
 * Usado pelas telas que ainda filtram e paginam no navegador.
 */
export function fetchAllPages<T>(http: HttpClient, path: `/${string}`): Observable<readonly T[]> {
  const fetchPage = (page: number): Observable<PagedResponseDto<T>> =>
    http.get<PagedResponseDto<T>>(buildApiUrl(path), {
      params: new HttpParams().set('page', String(page)).set('pageSize', String(MAX_API_PAGE_SIZE)),
    });

  return fetchPage(1).pipe(
    expand((response) => (response.page < response.totalPages ? fetchPage(response.page + 1) : EMPTY)),
    map((response) => response.items),
    reduce((all, items) => [...all, ...items], [] as readonly T[]),
  );
}
