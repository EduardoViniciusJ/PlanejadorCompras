import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * As páginas públicas viram HTML pronto no build, para buscadores e assistentes de IA
 * lerem o conteúdo sem executar JavaScript. Login e a área logada continuam só no navegador.
 */
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Prerender },
  { path: 'politica-de-privacidade', renderMode: RenderMode.Prerender },
  { path: 'termos-de-uso', renderMode: RenderMode.Prerender },
  { path: 'politica-de-cookies', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Client },
];
