import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { ThemeService } from '../../core/theme/theme.service';
import { GUIDE_PAGES, SEGMENT_PAGES } from '../../features/public-pages/public-pages';
import { AppIconComponent } from '../../shared/ui/app-icon/app-icon.component';
import { BrandMarkComponent } from '../../shared/ui/brand-mark/brand-mark.component';

interface PublicNavLink {
  readonly label: string;
  readonly fragment: string;
}

const NAV_LINKS: readonly PublicNavLink[] = [
  { label: 'IA', fragment: 'ia' },
  { label: 'Recursos', fragment: 'recursos' },
  { label: 'Como funciona', fragment: 'como-funciona' },
  { label: 'Dúvidas', fragment: 'duvidas' },
];

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, RouterLink, AppIconComponent, BrandMarkComponent],
  templateUrl: './public-layout.component.html',
})
export class PublicLayoutComponent {
  private readonly themeService = inject(ThemeService);

  protected readonly navLinks = NAV_LINKS;
  protected readonly segmentPages = SEGMENT_PAGES;
  protected readonly guidePages = GUIDE_PAGES;
  protected readonly currentYear = new Date().getFullYear();
  protected readonly currentTheme = this.themeService.theme;
  protected readonly themeButtonLabel = computed(() =>
    this.currentTheme() === 'dark' ? 'Ativar tema claro' : 'Ativar tema escuro',
  );

  protected toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  /** Fecha o menu do celular (um <details>) depois de escolher um link. */
  protected closeMenu(menu: HTMLDetailsElement): void {
    menu.open = false;
  }
}
