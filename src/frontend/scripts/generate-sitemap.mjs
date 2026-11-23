// Gera dist/frontend/browser/sitemap.xml a partir das páginas pré-renderizadas.
//
// Roda depois do `ng build` (veja o script "build" do package.json). Cada index.html
// pré-renderizado entra no sitemap pelo endereço do seu <link rel="canonical">; páginas
// com `noindex` ou sem canonical ficam de fora. Página pública nova entra sozinha,
// sem editar este arquivo.
import { readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const browserDir = fileURLToPath(new URL('../dist/frontend/browser/', import.meta.url));

function findPages(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) {
      return findPages(path);
    }
    return name === 'index.html' ? [path] : [];
  });
}

const escapeXml = (value) =>
  value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const urls = findPages(browserDir)
  .map((file) => {
    const html = readFileSync(file, 'utf8');
    const noindex = /<meta[^>]+name="robots"[^>]+content="[^"]*noindex/i.test(html);
    const canonical = html.match(/<link[^>]+rel="canonical"[^>]+href="([^"]+)"/i)?.[1];
    return {
      file: relative(browserDir, file).split(sep).join('/'),
      canonical: noindex ? null : canonical,
    };
  })
  .filter((page) => {
    if (!page.canonical) {
      console.warn(`sitemap: ${page.file} ficou de fora (noindex ou sem canonical).`);
    }
    return Boolean(page.canonical);
  })
  .map((page) => page.canonical)
  // Ordem alfabética (a página inicial, prefixo das outras, fica em primeiro), para o
  // arquivo não mudar à toa entre um build e outro.
  .sort((a, b) => a.localeCompare(b));

const unique = [...new Set(urls)];
const sitemap = [
  '<?xml version="1.0" encoding="UTF-8"?>',
  '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
  ...unique.map((url) => `  <url><loc>${escapeXml(url)}</loc></url>`),
  '</urlset>',
  '',
].join('\n');

writeFileSync(join(browserDir, 'sitemap.xml'), sitemap);
console.log(`sitemap: ${unique.length} páginas em sitemap.xml`);
