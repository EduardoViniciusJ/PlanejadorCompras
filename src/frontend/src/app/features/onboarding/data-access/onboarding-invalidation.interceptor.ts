import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';

/** Escritas que podem mudar um passo vindo da API (fornecedor, solicitação ou pedido). */
const ONBOARDING_RESOURCE_PATTERN =
  /\/api\/(suppliers|purchase-orders)(\/|\?|$)|\/quotation-requests(\/|\?|$)/;

/**
 * Contador de versões do progresso dos primeiros passos. Fica separado do
 * `OnboardingService` para o interceptor (que entra no pacote inicial) não puxar
 * os serviços de fornecedores, solicitações e pedidos para a landing.
 */
@Injectable({ providedIn: 'root' })
export class OnboardingInvalidation {
  private readonly revisionState = signal(0);

  readonly revision = this.revisionState.asReadonly();

  invalidate(): void {
    this.revisionState.update((value) => value + 1);
  }
}

/**
 * Em vez de buscar o progresso a cada navegação, o estado dos primeiros passos
 * só é marcado como desatualizado quando uma escrita relevante dá certo.
 */
export const onboardingInvalidationInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.method === 'GET' || !ONBOARDING_RESOURCE_PATTERN.test(request.url)) {
    return next(request);
  }

  const invalidation = inject(OnboardingInvalidation);

  return next(request).pipe(
    tap((event) => {
      if (event instanceof HttpResponse && event.ok) {
        invalidation.invalidate();
      }
    }),
  );
};
