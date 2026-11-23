import { RenderMode } from '@angular/ssr';

import { routes } from '../../app.routes';
import { serverRoutes } from '../../app.routes.server';
import { GUIDE_PAGE_CONTENT } from './content/guide-pages.content';
import { SEGMENT_PAGE_CONTENT } from './content/segment-pages.content';
import { PUBLIC_CONTENT_PAGES } from './public-pages';

describe('Páginas públicas de busca', () => {
  const publicChildren =
    routes.find((route) => route.path === '' && route.children)?.children ?? [];

  for (const page of PUBLIC_CONTENT_PAGES) {
    it(`should route, describe and prerender /${page.slug}`, () => {
      const route = publicChildren.find((child) => child.path === page.slug);

      expect(route).toBeDefined();
      expect(route?.title).toBe(page.title);
      expect(route?.data?.['description']).toBe(page.description);
      expect(route?.data?.['noindex']).toBeUndefined();
      expect(route?.loadComponent).toBeDefined();

      const serverRoute = serverRoutes.find((entry) => entry.path === page.slug);
      expect(serverRoute?.renderMode).toBe(RenderMode.Prerender);
    });
  }

  it('should have content for every registered page', () => {
    const slugs = PUBLIC_CONTENT_PAGES.map((page) => page.slug).sort();
    const contentSlugs = [
      ...Object.keys(SEGMENT_PAGE_CONTENT),
      ...Object.keys(GUIDE_PAGE_CONTENT),
    ].sort();

    expect(contentSlugs).toEqual(slugs);
  });

  it('should keep titles and descriptions unique and within search result size', () => {
    const titles = PUBLIC_CONTENT_PAGES.map((page) => page.title);
    const descriptions = PUBLIC_CONTENT_PAGES.map((page) => page.description);

    expect(new Set(titles).size).toBe(titles.length);
    expect(new Set(descriptions).size).toBe(descriptions.length);
    for (const description of descriptions) {
      expect(description.length).toBeGreaterThan(80);
      expect(description.length).toBeLessThanOrEqual(180);
    }
  });

  it('should keep the catch-all route after the new pages', () => {
    expect(serverRoutes.at(-1)?.path).toBe('**');
    expect(serverRoutes.at(-1)?.renderMode).toBe(RenderMode.Client);
  });
});
