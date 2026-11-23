import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PUBLIC_CONTENT_PAGES } from '../../features/public-pages/public-pages';
import { PublicLayoutComponent } from './public-layout.component';

describe('PublicLayoutComponent', () => {
  beforeEach(async () => {
    localStorage.clear();
    localStorage.setItem('planejador-theme', 'dark');

    await TestBed.configureTestingModule({
      imports: [PublicLayoutComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-app-theme');
    document.documentElement.style.colorScheme = '';
  });

  it('should link the header actions to login and registration', async () => {
    const fixture = TestBed.createComponent(PublicLayoutComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    const hrefs = Array.from(element.querySelectorAll('header a')).map((link) =>
      link.getAttribute('href'),
    );

    expect(hrefs).toContain('/login');
    expect(hrefs).toContain('/cadastro');
  });

  it('should link every segment page and guide from the footer', async () => {
    const fixture = TestBed.createComponent(PublicLayoutComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const hrefs = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('footer a')).map(
      (link) => link.getAttribute('href'),
    );

    for (const page of PUBLIC_CONTENT_PAGES) {
      expect(hrefs).toContain(`/${page.slug}`);
    }
  });

  it('should toggle and persist the theme', async () => {
    const fixture = TestBed.createComponent(PublicLayoutComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(document.documentElement.getAttribute('data-theme')).toBe('claude-dark');

    const toggle = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.theme-toggle',
    );
    toggle?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(document.documentElement.getAttribute('data-theme')).toBe('claude');
    expect(document.documentElement.getAttribute('data-app-theme')).toBe('light');
    expect(localStorage.getItem('planejador-theme')).toBe('light');
  });
});
