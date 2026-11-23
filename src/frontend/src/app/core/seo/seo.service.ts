import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, NavigationEnd, Router, TitleStrategy } from '@angular/router';
import { filter } from 'rxjs';

import { SITE_URL } from './site-url';

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
  private readonly titleStrategy = inject(TitleStrategy);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

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

  /**
   * Endereço absoluto de um caminho do site. No pré-render usa `SITE_URL`; no navegador,
   * a origem da página aberta (o domínio real em produção).
   */
  absoluteUrl(path: string): string {
    const origin = this.isBrowser ? this.document.location.origin : SITE_URL;
    const cleanPath = path.split(/[?#]/)[0];
    return `${origin.replace(/\/$/, '')}${cleanPath.startsWith('/') ? '' : '/'}${cleanPath}`;
  }

  private apply(route: ActivatedRouteSnapshot): void {
    const data = route.data as SeoRouteData;
    const description = data.description ?? DEFAULT_DESCRIPTION;
    // O título da rota sai da própria rota: no NavigationEnd o <title> ainda pode ser o anterior.
    const pageTitle =
      this.titleStrategy.buildTitle(this.router.routerState.snapshot) ?? this.title.getTitle();

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
    this.applyCanonical(data.noindex ? null : this.absoluteUrl(this.router.url));
  }

  /** Página indexável ganha canonical e `og:url`; página com `noindex` fica sem os dois. */
  private applyCanonical(url: string | null): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');

    if (!url) {
      link?.remove();
      this.meta.removeTag('property="og:url"');
      return;
    }

    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'canonical';
      this.document.head.appendChild(link);
    }
    link.href = url;
    this.meta.updateTag({ property: 'og:url', content: url });
  }

  private deepestRoute(route: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let current = route;
    while (current.firstChild) {
      current = current.firstChild;
    }
    return current;
  }
}
