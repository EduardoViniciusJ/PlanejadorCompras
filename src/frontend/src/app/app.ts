import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SeoService } from './core/seo/seo.service';
import { ThemeService } from './core/theme/theme.service';

/** Raiz: só hospeda o roteador. Cada área (pública, login, app) tem o próprio layout como rota. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  // Injetado aqui para o tema ser aplicado antes da primeira tela.
  private readonly themeService = inject(ThemeService);

  constructor() {
    inject(SeoService).start();
  }
}
