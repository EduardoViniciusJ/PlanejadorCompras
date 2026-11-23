import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { GUIDE_PAGES, SEGMENT_PAGES } from '../../public-pages';

/**
 * Fim comum das páginas de busca: chamada para criar a conta e links para as outras
 * páginas (segmentos e guias), que ajudam quem lê e os buscadores a achar o resto do site.
 */
@Component({
  selector: 'app-page-closing',
  imports: [RouterLink, AppIconComponent],
  template: `
    <section class="border-t border-base-300">
      <div class="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:py-24">
        <div class="max-w-2xl">
          <h2 class="font-display text-3xl font-semibold tracking-tight sm:text-4xl">
            {{ title() }}
          </h2>
          <p class="mt-4 text-lg text-base-content/70">{{ text() }}</p>
          <div class="mt-8 flex flex-col gap-3 sm:flex-row sm:items-center">
            <a class="btn btn-primary btn-lg w-full sm:w-auto" routerLink="/cadastro">
              Criar conta grátis
              <app-icon name="arrow-right" [size]="20" />
            </a>
            <a class="btn btn-ghost btn-lg w-full sm:w-auto" routerLink="/">Conhecer o produto</a>
          </div>
        </div>

        <div class="mt-16 grid gap-10 border-t border-base-300 pt-10 sm:grid-cols-2">
          <nav aria-label="Páginas por segmento">
            <h2 class="text-sm font-semibold">Por segmento</h2>
            <ul class="mt-3 grid gap-2 text-sm">
              @for (page of segments(); track page.slug) {
                <li>
                  <a class="link link-hover text-base-content/70" [routerLink]="'/' + page.slug">
                    Planejador de Compras para {{ page.label.toLowerCase() }}
                  </a>
                </li>
              }
            </ul>
          </nav>
          <nav aria-label="Guias de compras">
            <h2 class="text-sm font-semibold">Guias de compras</h2>
            <ul class="mt-3 grid gap-2 text-sm">
              @for (page of guides(); track page.slug) {
                <li>
                  <a class="link link-hover text-base-content/70" [routerLink]="'/' + page.slug">
                    {{ page.label }}
                  </a>
                </li>
              }
            </ul>
          </nav>
        </div>
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageClosingComponent {
  readonly title = input.required<string>();
  readonly text = input.required<string>();
  /** Página aberta, que fica fora da lista de links. */
  readonly currentSlug = input.required<string>();

  protected readonly segments = computed(() =>
    SEGMENT_PAGES.filter((page) => page.slug !== this.currentSlug()),
  );
  protected readonly guides = computed(() =>
    GUIDE_PAGES.filter((page) => page.slug !== this.currentSlug()),
  );
}
