#!/bin/sh
# Troca o endereço provisório do pré-render (src/app/core/seo/site-url.ts) pelo domínio
# público informado em SITE_URL, nos HTML, XML e TXT publicados: canonical, og:url,
# JSON-LD, sitemap.xml e robots.txt. Sem SITE_URL, nada muda.
set -eu

placeholder='https://planejadordecompras.com'
site_url="${SITE_URL:-}"
site_url="${site_url%/}"

if [ -z "$site_url" ]; then
    echo "50-site-url.sh: SITE_URL não definido; canonical e sitemap ficam com $placeholder" >&2
    exit 0
fi

case "$site_url" in
    http://*|https://*) ;;
    *)
        echo "50-site-url.sh: SITE_URL precisa começar com http:// ou https://" >&2
        exit 1
        ;;
esac

# Escapa os caracteres especiais do sed na substituição.
escaped=$(printf '%s' "$site_url" | sed 's/[&|\\]/\\&/g')

find /usr/share/nginx/html -type f \( -name '*.html' -o -name '*.xml' -o -name '*.txt' \) \
    -exec sed -i "s|$placeholder|$escaped|g" {} +
