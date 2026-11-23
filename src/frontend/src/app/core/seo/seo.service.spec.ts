import { Component, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { SeoService } from './seo.service';
import { SITE_URL } from './site-url';

@Component({ template: '' })
class EmptyPageComponent {}

const TEST_ROUTES = [
  {
    path: 'para-mercados',
    title: 'Cotação para mercados | Planejador de Compras',
    data: { description: 'Descrição da página de mercados.' },
    component: EmptyPageComponent,
  },
  {
    path: 'login',
    title: 'Entrar | Planejador de Compras',
    data: { noindex: true },
    component: EmptyPageComponent,
  },
];

const metaContent = (selector: string): string | null =>
  document.head.querySelector(`meta[${selector}]`)?.getAttribute('content') ?? null;

const canonical = (): string | null =>
  document.head.querySelector('link[rel="canonical"]')?.getAttribute('href') ?? null;

describe('SeoService', () => {
  const setup = async (platform: 'browser' | 'server' = 'browser') => {
    TestBed.configureTestingModule({
      providers: [provideRouter(TEST_ROUTES), { provide: PLATFORM_ID, useValue: platform }],
    });
    const seo = TestBed.inject(SeoService);
    seo.start();
    return { seo, router: TestBed.inject(Router) };
  };

  afterEach(() => {
    TestBed.resetTestingModule();
    document.head.querySelector('link[rel="canonical"]')?.remove();
    for (const selector of [
      'name="description"',
      'property="og:title"',
      'property="og:url"',
      'name="robots"',
    ]) {
      document.head.querySelector(`meta[${selector}]`)?.remove();
    }
  });

  it('should apply title, description and canonical of the opened page', async () => {
    const { router } = await setup();

    await router.navigateByUrl('/para-mercados?origem=teste#fluxo');

    const expectedUrl = `${document.location.origin}/para-mercados`;
    expect(metaContent('name="description"')).toBe('Descrição da página de mercados.');
    expect(metaContent('property="og:title"')).toBe(
      'Cotação para mercados | Planejador de Compras',
    );
    expect(metaContent('name="robots"')).toBe('index, follow');
    expect(canonical()).toBe(expectedUrl);
    expect(metaContent('property="og:url"')).toBe(expectedUrl);
  });

  it('should drop canonical and og:url on noindex pages', async () => {
    const { router } = await setup();

    await router.navigateByUrl('/para-mercados');
    expect(canonical()).not.toBeNull();

    await router.navigateByUrl('/login');
    expect(metaContent('name="robots"')).toBe('noindex, nofollow');
    expect(canonical()).toBeNull();
    expect(metaContent('property="og:url"')).toBeNull();
  });

  it('should use the public site address while prerendering', async () => {
    const { seo, router } = await setup('server');

    expect(seo.absoluteUrl('/para-lojas')).toBe(`${SITE_URL}/para-lojas`);
    expect(seo.absoluteUrl('/')).toBe(`${SITE_URL}/`);

    await router.navigateByUrl('/para-mercados');
    expect(canonical()).toBe(`${SITE_URL}/para-mercados`);
  });
});
