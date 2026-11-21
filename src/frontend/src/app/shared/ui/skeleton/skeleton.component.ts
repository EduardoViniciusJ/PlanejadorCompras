import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type SkeletonVariant = 'cards' | 'table' | 'detail' | 'form';

/**
 * Placeholder de carregamento. Mostra a forma do conteúdo que está chegando
 * em vez de um spinner, para a página não "pular" quando os dados aparecem.
 */
@Component({
  selector: 'app-skeleton',
  template: `
    <div class="w-full" role="status" aria-busy="true">
      <span class="visually-hidden">{{ label() }}</span>

      @switch (variant()) {
        @case ('cards') {
          <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-hidden="true">
            @for (item of items(); track item) {
              <div class="rounded-box border border-base-300 bg-base-100 p-5">
                <div class="skeleton h-5 w-2/3"></div>
                <div class="skeleton mt-3 h-4 w-full"></div>
                <div class="skeleton mt-2 h-4 w-4/5"></div>
                <div class="mt-6 flex gap-2">
                  <div class="skeleton h-9 w-24"></div>
                  <div class="skeleton h-9 w-9"></div>
                </div>
              </div>
            }
          </div>
        }
        @case ('table') {
          <div class="rounded-box border border-base-300 bg-base-100" aria-hidden="true">
            <div class="flex gap-4 border-b border-base-300 p-4">
              <div class="skeleton h-4 w-1/4"></div>
              <div class="skeleton h-4 w-1/6"></div>
              <div class="skeleton h-4 w-1/6"></div>
              <div class="skeleton ml-auto h-4 w-16"></div>
            </div>
            @for (item of items(); track item) {
              <div class="flex items-center gap-4 border-b border-base-300 p-4 last:border-b-0">
                <div class="skeleton h-4 w-1/3"></div>
                <div class="skeleton h-4 w-1/6"></div>
                <div class="skeleton h-4 w-1/5"></div>
                <div class="skeleton ml-auto h-8 w-20"></div>
              </div>
            }
          </div>
        }
        @case ('detail') {
          <div class="grid gap-4" aria-hidden="true">
            <div class="skeleton h-8 w-1/2"></div>
            <div class="skeleton h-4 w-1/3"></div>
            <div class="grid gap-4 sm:grid-cols-3">
              <div class="skeleton h-24"></div>
              <div class="skeleton h-24"></div>
              <div class="skeleton h-24"></div>
            </div>
            <div class="skeleton h-64 w-full"></div>
          </div>
        }
        @case ('form') {
          <div class="grid gap-5" aria-hidden="true">
            @for (item of items(); track item) {
              <div>
                <div class="skeleton h-4 w-32"></div>
                <div class="skeleton mt-2 h-11 w-full"></div>
              </div>
            }
            <div class="skeleton h-11 w-40"></div>
          </div>
        }
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
  readonly variant = input<SkeletonVariant>('cards');
  readonly count = input(6);
  readonly label = input('Carregando…');

  protected readonly items = computed(() =>
    Array.from({ length: Math.max(1, this.count()) }, (_, index) => index),
  );
}
