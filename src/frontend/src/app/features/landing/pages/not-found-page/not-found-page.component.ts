import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, AppIconComponent],
  template: `
    <section class="mx-auto w-full max-w-xl px-4 py-24 text-center sm:px-6">
      <p class="font-mono text-sm font-semibold text-accent">Erro 404</p>
      <h1 class="mt-3 font-display text-4xl font-semibold tracking-tight">
        Página não encontrada
      </h1>
      <p class="mt-4 text-lg text-base-content/70">
        O endereço pode ter mudado ou o link está incompleto.
      </p>
      <div class="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
        <a class="btn btn-primary" routerLink="/">
          <app-icon name="arrow-left" [size]="18" />
          Voltar para o início
        </a>
        <a class="btn btn-ghost" routerLink="/app">Abrir o sistema</a>
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPageComponent {}
