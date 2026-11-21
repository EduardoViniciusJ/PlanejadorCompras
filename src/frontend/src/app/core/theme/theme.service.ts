import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { effect, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

export type AppTheme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'planejador-theme';

/** Nome do tema do DaisyUI (definido em tailwind.css) para cada modo. */
const DAISY_THEME: Record<AppTheme, string> = {
  light: 'claude',
  dark: 'claude-dark',
};

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly themeState = signal<AppTheme>(this.resolveInitialTheme());

  readonly theme = this.themeState.asReadonly();

  constructor() {
    effect(() => {
      this.applyTheme(this.themeState());
    });
  }

  setTheme(theme: AppTheme): void {
    this.themeState.set(theme);
    this.storeTheme(theme);
  }

  toggleTheme(): void {
    this.setTheme(this.themeState() === 'dark' ? 'light' : 'dark');
  }

  /** Escolha salva; sem ela, segue a preferência do sistema. */
  private resolveInitialTheme(): AppTheme {
    return this.readStoredTheme() ?? (this.systemPrefersDark() ? 'dark' : 'light');
  }

  private systemPrefersDark(): boolean {
    const browserWindow = this.document.defaultView;

    return (
      typeof browserWindow?.matchMedia === 'function' &&
      browserWindow.matchMedia('(prefers-color-scheme: dark)').matches
    );
  }

  private readStoredTheme(): AppTheme | null {
    const value = this.getStorage()?.getItem(THEME_STORAGE_KEY);

    if (value === 'light' || value === 'dark') {
      return value;
    }

    return null;
  }

  private storeTheme(theme: AppTheme): void {
    try {
      this.getStorage()?.setItem(THEME_STORAGE_KEY, theme);
    } catch {
      // Sem armazenamento (modo privado): o tema vale só para esta visita.
    }
  }

  private getStorage(): Storage | null {
    const browserWindow = this.document.defaultView;

    if (!browserWindow) {
      return null;
    }

    try {
      return browserWindow.localStorage;
    } catch {
      return null;
    }
  }

  private applyTheme(theme: AppTheme): void {
    // No pré-render o HTML fica sem tema; o script do index.html escolhe antes da primeira pintura.
    if (!this.isBrowser) {
      return;
    }

    const root = this.document.documentElement;

    // `data-theme` troca o tema do DaisyUI; `data-app-theme` troca as variáveis `--app-*`.
    root.setAttribute('data-theme', DAISY_THEME[theme]);
    root.setAttribute('data-app-theme', theme);
    root.style.colorScheme = theme;
  }
}
