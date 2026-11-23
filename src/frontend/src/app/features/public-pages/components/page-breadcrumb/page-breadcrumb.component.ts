import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Trilha "Início / página atual" mostrada no topo das páginas de busca. */
@Component({
  selector: 'app-page-breadcrumb',
  imports: [RouterLink],
  template: `
    <nav class="breadcrumbs p-0 text-sm text-base-content/60" aria-label="Trilha de navegação">
      <ul>
        <li><a class="link link-hover" routerLink="/">Início</a></li>
        <li>
          <span aria-current="page">{{ label() }}</span>
        </li>
      </ul>
    </nav>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageBreadcrumbComponent {
  readonly label = input.required<string>();
}
