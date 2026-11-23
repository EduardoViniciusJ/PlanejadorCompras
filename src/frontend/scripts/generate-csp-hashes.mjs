#!/usr/bin/env node
// Gera as fontes de script da Content-Security-Policy a partir do build.
//
// A política não usa 'unsafe-inline' para scripts. Os poucos trechos inline que o build
// produz (o script de tema do index.html, o replay de eventos do Angular nas páginas
// pré-renderizadas e o onload do CSS crítico) são liberados um a um pelo hash SHA-256 do
// conteúdo. Como esse conteúdo muda a cada versão do Angular ou do index.html, os hashes
// são calculados aqui, na hora do build da imagem, e nunca escritos à mão.
//
// Uso: node scripts/generate-csp-hashes.mjs dist/frontend/browser
// Saída (stdout): uma linha com as fontes, por exemplo
//   'sha256-AAA=' 'sha256-BBB=' 'unsafe-hashes' 'sha256-CCC='

import { createHash } from 'node:crypto';
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

// Blocos de dados (application/ld+json, application/json) não são executados pelo navegador
// e por isso não passam pela CSP: só scripts executáveis entram na lista.
const EXECUTABLE_SCRIPT_TYPES = new Set(['', 'text/javascript', 'application/javascript', 'module']);

const root = process.argv[2];

if (!root) {
  console.error('Uso: node scripts/generate-csp-hashes.mjs <pasta do build>');
  process.exit(1);
}

const scriptHashes = new Set();
const handlerHashes = new Set();
const htmlFiles = findHtmlFiles(root);

if (htmlFiles.length === 0) {
  console.error(`Nenhum arquivo HTML encontrado em ${root}.`);
  process.exit(1);
}

for (const file of htmlFiles) {
  const html = readFileSync(file, 'utf8');

  for (const [, attributes, body] of html.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/gi)) {
    if (/\bsrc\s*=/i.test(attributes) || body.length === 0) {
      continue;
    }

    const type = (attributes.match(/\btype\s*=\s*["']?([^"'\s>]+)/i)?.[1] ?? '').toLowerCase();

    if (EXECUTABLE_SCRIPT_TYPES.has(type)) {
      scriptHashes.add(sha256(body));
    }
  }

  for (const [tag] of html.matchAll(/<[a-z][^>]*>/gi)) {
    for (const [, , value] of tag.matchAll(/\son[a-z]+\s*=\s*(["'])([\s\S]*?)\1/gi)) {
      handlerHashes.add(sha256(decodeHtmlEntities(value)));
    }
  }
}

const sources = [...scriptHashes].sort();

if (handlerHashes.size > 0) {
  // 'unsafe-hashes' permite atributos de evento inline, mas só os que tiverem um destes hashes.
  sources.push("'unsafe-hashes'", ...[...handlerHashes].sort());
}

process.stdout.write(`${sources.join(' ')}\n`);

function findHtmlFiles(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);

    if (entry.isDirectory()) {
      return findHtmlFiles(path);
    }

    return entry.name.endsWith('.html') ? [path] : [];
  });
}

function sha256(content) {
  return `'sha256-${createHash('sha256').update(content, 'utf8').digest('base64')}'`;
}

function decodeHtmlEntities(value) {
  return value
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&apos;', "'")
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .replaceAll('&amp;', '&');
}
