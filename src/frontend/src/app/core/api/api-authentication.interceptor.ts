import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { SessionRefreshService } from '../auth/session-refresh.service';
import { isApiUrl } from './api-url';

/** Rotas de autenticação em que um 401 é a resposta final (credencial errada, sessão encerrada). */
const AUTH_PATHS_WITHOUT_RENEWAL = [
  '/api/auth/login',
  '/api/auth/google-code',
  '/api/auth/register',
  '/api/auth/confirm-email',
  '/api/auth/forgot-password',
  '/api/auth/reset-password',
  '/api/auth/refresh',
  '/api/auth/logout',
];

/**
 * O token de acesso dura poucos minutos. Quando a API responde 401, a sessão é renovada uma
 * única vez e a requisição original é repetida, sem a pessoa perceber nem perder o que digitou.
 * Se a renovação falhar, ou se a requisição repetida ainda voltar 401, a sessão acabou de
 * verdade: vai para o login. A requisição repetida nunca dispara outra renovação.
 */
export const apiAuthenticationInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url) || isAuthPathWithoutRenewal(request.url)) {
    return next(request);
  }

  const router = inject(Router);
  const sessionRefresh = inject(SessionRefreshService);
  const requestStartedAt = Date.now();
  // /api/auth/me é a pergunta "estou logado?": quem decide para onde ir é quem perguntou.
  const redirectsToLogin = !request.url.includes('/api/auth/');

  const endSession = (): void => {
    if (!redirectsToLogin) {
      return;
    }

    const returnUrl = router.url.startsWith('/app') ? router.url : '/app';
    void router.navigate(['/login'], {
      queryParams: { returnUrl, reason: 'session-expired' },
    });
  };

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!isUnauthorized(error)) {
        return throwError(() => error);
      }

      return sessionRefresh.renew(requestStartedAt).pipe(
        catchError((renewalError: unknown) => {
          // Só o 401 da renovação significa sessão encerrada. Falha de rede ou erro do
          // servidor não desloga ninguém: a tela mostra o erro e a pessoa tenta de novo.
          if (isUnauthorized(renewalError)) {
            endSession();
          }

          return throwError(() => error);
        }),
        switchMap(() =>
          next(request).pipe(
            catchError((retryError: unknown) => {
              if (isUnauthorized(retryError)) {
                endSession();
              }

              return throwError(() => retryError);
            }),
          ),
        ),
      );
    }),
  );
};

function isUnauthorized(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}

function isAuthPathWithoutRenewal(url: string): boolean {
  return AUTH_PATHS_WITHOUT_RENEWAL.some((path) => url.includes(path));
}
