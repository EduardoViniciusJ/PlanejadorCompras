import { Route } from '@angular/router';

import { GUIDE_PAGES, PublicPageEntry, PublicPageRouteData, SEGMENT_PAGES } from './public-pages';

const toRoute = (page: PublicPageEntry, loadComponent: Route['loadComponent']): Route => ({
  path: page.slug,
  title: page.title,
  data: { slug: page.slug, description: page.description } satisfies PublicPageRouteData,
  loadComponent,
});

/** Rotas das páginas por segmento e dos guias, filhas do layout público. */
export const PUBLIC_PAGE_ROUTES: Route[] = [
  ...SEGMENT_PAGES.map((page) =>
    toRoute(page, () =>
      import('./pages/segment-page/segment-page.component').then(
        ({ SegmentPageComponent }) => SegmentPageComponent,
      ),
    ),
  ),
  ...GUIDE_PAGES.map((page) =>
    toRoute(page, () =>
      import('./pages/guide-page/guide-page.component').then(
        ({ GuidePageComponent }) => GuidePageComponent,
      ),
    ),
  ),
];
