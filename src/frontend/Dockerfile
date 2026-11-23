FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY docker-entrypoint.d/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY docker-entrypoint.d/50-site-url.sh /docker-entrypoint.d/50-site-url.sh
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh /docker-entrypoint.d/50-site-url.sh
EXPOSE 80
