FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production
# Hashes dos scripts inline do build, usados na Content-Security-Policy.
RUN node scripts/generate-csp-hashes.mjs dist/frontend/browser > csp-script-hashes.txt

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY --from=build /app/csp-script-hashes.txt /etc/nginx/csp-script-hashes.txt
COPY docker-entrypoint.d/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY docker-entrypoint.d/45-security-headers.sh /docker-entrypoint.d/45-security-headers.sh
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh /docker-entrypoint.d/45-security-headers.sh
EXPOSE 80
