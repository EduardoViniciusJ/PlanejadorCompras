import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/** Dados de SEO que uma rota pode declarar em `data`. */
export interface SeoRouteData {
  /** Texto do resultado de busca e da prévia de link. */
  readonly description?: string;
  /** Páginas que não devem aparecer em buscadores (login, área logada). */
  readonly noindex?: boolean;
}

const DEFAULT_DESCRIPTION =
  'Sistema de cotação e pedido de compra para mercados, restaurantes, lojas e clínicas. ' +
  'Envie a cotação em PDF, compare fornecedores no mapa de preços e emita o pedido, com ajuda de IA.';

/**
 * Mantém descrição, Open Graph e `robots` iguais à página aberta.
 * Roda também no pré-render, então o HTML estático já sai com as tags certas.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly document = inject(DOCUMENT);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Começa a acompanhar a navegação. Chamado uma vez, pelo componente raiz. */
  start(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.apply(this.deepestRoute(this.router.routerState.snapshot.root)));
  }

  /** Publica dados estruturados (JSON-LD) no `<head>`; devolve a função que os remove. */
  setStructuredData(id: string, data: object): () => void {
    this.document.getElementById(id)?.remove();

    const script = this.document.createElement('script');
    script.id = id;
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(data);
    this.document.head.appendChild(script);

    return () => script.remove();
  }

  private apply(route: ActivatedRouteSnapshot): void {
    const data = route.data as SeoRouteData;
    const description = data.description ?? DEFAULT_DESCRIPTION;
    const pageTitle = this.title.getTitle();

    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
    this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({
      name: 'robots',
      content: data.noindex ? 'noindex, nofollow' : 'index, follow',
    });
  }

  private deepestRoute(route: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let current = route;
    while (current.firstChild) {
      current = current.firstChild;
    }
    return current;
  }
}
