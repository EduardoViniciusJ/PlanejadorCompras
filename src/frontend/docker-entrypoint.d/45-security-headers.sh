#!/bin/sh
# Gera, na subida do contêiner, os cabeçalhos de segurança que o nginx envia.
# A Content-Security-Policy depende de duas coisas que só existem fora do nginx.conf:
#   - a origem da API (API_BASE_URL), definida por ambiente;
#   - os hashes dos scripts inline do build (/etc/nginx/csp-script-hashes.txt), gerados
#     no build da imagem por scripts/generate-csp-hashes.mjs.
set -eu

api_base_url="${API_BASE_URL:-}"
snippets_dir="${NGINX_SNIPPETS_DIR:-/etc/nginx/snippets}"
script_hashes_file="${CSP_SCRIPT_HASHES_FILE:-/etc/nginx/csp-script-hashes.txt}"

# Só a origem (esquema, host e porta) entra na política. Qualquer coisa fora desse formato é
# recusada: o valor vai para dentro de um arquivo de configuração do nginx.
api_origin=""
if [ -n "$api_base_url" ]; then
    api_origin="$(printf '%s' "$api_base_url" \
        | sed -n -E 's#^(https?://[A-Za-z0-9.-]+(:[0-9]+)?)(/.*)?$#\1#p')"

    if [ -z "$api_origin" ]; then
        echo "45-security-headers: API_BASE_URL invalida; esperado http(s)://host[:porta]." >&2
        exit 1
    fi
fi

script_hashes=""
if [ -f "$script_hashes_file" ]; then
    script_hashes="$(tr -d '\r\n' < "$script_hashes_file")"

    if [ -n "$script_hashes" ] \
        && ! printf '%s' "$script_hashes" | grep -Eq "^('(sha256-[A-Za-z0-9+/=]+|unsafe-hashes)' ?)+$"; then
        echo "45-security-headers: $script_hashes_file tem conteudo inesperado." >&2
        exit 1
    fi
else
    echo "45-security-headers: $script_hashes_file nao encontrado; scripts inline serao bloqueados." >&2
fi

# accounts.google.com/gsi/*: origens exigidas pelo Google Identity Services (login com Google).
csp="default-src 'self'"
csp="$csp; script-src 'self' https://accounts.google.com/gsi/client${script_hashes:+ $script_hashes}"
csp="$csp; style-src 'self' 'unsafe-inline' https://accounts.google.com/gsi/style"
csp="$csp; img-src 'self' data:"
csp="$csp; font-src 'self' data:"
csp="$csp; connect-src 'self'${api_origin:+ $api_origin} https://accounts.google.com/gsi/"
csp="$csp; frame-src https://accounts.google.com/gsi/"
csp="$csp; frame-ancestors 'none'"
csp="$csp; base-uri 'self'"
csp="$csp; form-action 'self'"
csp="$csp; object-src 'none'"

mkdir -p "$snippets_dir"

# Enviados em toda resposta (HTML, arquivos estáticos e erros).
cat > "$snippets_dir/security-headers.conf" <<'EOF'
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Strict-Transport-Security "max-age=31536000" always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
add_header Cross-Origin-Opener-Policy "same-origin-allow-popups" always;
EOF

# Enviado junto com as páginas HTML.
cat > "$snippets_dir/content-security-policy.conf" <<EOF
add_header Content-Security-Policy "$csp" always;
EOF
