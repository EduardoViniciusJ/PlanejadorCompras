/**
 * Endereço público do site usado no pré-render (canonical, `og:url`, JSON-LD, sitemap).
 *
 * O domínio de produção ainda não foi definido. Este valor é um marcador: na subida do
 * contêiner, `docker-entrypoint.d/50-site-url.sh` troca o marcador pelo valor da variável
 * `SITE_URL` em todos os HTML, XML e TXT publicados. No navegador o canonical usa a origem
 * da própria página, então o marcador nunca chega a um link clicável.
 */
export const SITE_URL = 'https://planejador-de-compras.example';
