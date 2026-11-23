import { HttpBackend, HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { finalize, map, Observable, of, shareReplay, tap } from 'rxjs';

import { buildApiUrl } from '../api/api-url';

/**
 * Renova a sessão trocando o refresh token (cookie HttpOnly) por um novo par de tokens.
 *
 * Só existe uma renovação em andamento por vez: requisições que falham com 401 ao mesmo tempo
 * esperam a mesma chamada, porque o refresh token é rotativo e só pode ser usado uma vez.
 */
@Injectable({
  providedIn: 'root',
})
export class SessionRefreshService {
  // HttpBackend direto: a renovação não passa pelos interceptors, então um 401 dela nunca
  // dispara outra renovação.
  private readonly http = new HttpClient(inject(HttpBackend));
  private renewalInProgress: Observable<void> | null = null;
  private lastRenewedAt = 0;

  /**
   * @param requestStartedAt Momento (Date.now()) em que a requisição que recebeu 401 foi enviada.
   * Se a sessão já foi renovada depois disso, a requisição só precisa ser repetida.
   */
  renew(requestStartedAt: number): Observable<void> {
    if (this.renewalInProgress) {
      return this.renewalInProgress;
    }

    if (this.lastRenewedAt > requestStartedAt) {
      return of(undefined);
    }

    this.renewalInProgress = this.http
      .post<void>(buildApiUrl('/api/auth/refresh'), null, {
        withCredentials: true,
        headers: { 'X-Requested-With': 'XmlHttpRequest' },
      })
      .pipe(
        map(() => undefined),
        tap(() => {
          this.lastRenewedAt = Date.now();
        }),
        finalize(() => {
          this.renewalInProgress = null;
        }),
        // A chamada acontece uma vez só e o resultado vale para todos que estão esperando,
        // mesmo que quem pediu primeiro desista no meio.
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.renewalInProgress;
  }
}
