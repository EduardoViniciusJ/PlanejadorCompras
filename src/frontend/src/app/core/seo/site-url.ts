/**
 * Endereço público do site usado no pré-render (canonical, `og:url`, JSON-LD, sitemap).
 *
 * Para publicar em outro endereço (por exemplo, um ambiente de homologação), defina a
 * variável `SITE_URL` no contêiner: `docker-entrypoint.d/50-site-url.sh` troca este valor
 * nos HTML, XML e TXT publicados. No navegador o canonical usa a origem da própria página.
 */
export const SITE_URL = 'https://planejadordecompras.com';
