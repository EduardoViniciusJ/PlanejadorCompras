import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Marca do produto: um carrinho sem fundo. O traço usa a cor do texto ao redor,
 * então se adapta ao tema claro e ao escuro; só a roda leva a cor de destaque.
 */
@Component({
  selector: 'app-brand-mark',
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="4.5 6.5 22 21"
      fill="none"
      aria-hidden="true"
      focusable="false"
    >
      <path
        d="M6.8 8.8h2.7l2.1 10.4a1.7 1.7 0 0 0 1.7 1.3h7.5a1.7 1.7 0 0 0 1.6-1.3l1.6-6.7H10.6"
        stroke="currentColor"
        stroke-width="2.2"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
      <circle cx="13.6" cy="24.4" r="1.7" fill="currentColor" />
      <circle cx="20.6" cy="24.4" r="1.7" fill="var(--color-accent)" />
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      flex: 0 0 auto;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandMarkComponent {
  readonly size = input(32);
}
