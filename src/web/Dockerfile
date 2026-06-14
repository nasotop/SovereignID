# ETAPA 1: Construcción (Node.js)
FROM node:22-alpine AS build

WORKDIR /app

# Instalar dependencias de compilación si es necesario
RUN apk add --no-cache python3 make g++

# Copiar solo archivos de dependencias primero (para aprovechar cache de Docker)
COPY package*.json ./
COPY .npmrc* ./

# Usar npm install (necesitamos las dependencias de desarrollo para compilar Angular)
RUN npm install

# Copiar el código fuente
COPY . .

# Compilar Angular en modo producción
RUN npm run build --configuration=production

# ¡AQUÍ ESTÁ EL CAMBIO GUARDADO! Buscamos la carpeta sovereignid-web
RUN test -d dist/sovereignid-web/browser || (ls -la dist/ && exit 1)

# ETAPA 2: Servidor Web / API Gateway Local (Nginx)
FROM nginx:1.27-alpine

LABEL maintainer="SovereignID Team" \
      description="Frontend Angular + Nginx Reverse Proxy" \
      version="1.0" \
      org.opencontainers.image.source="https://github.com/nasotop/SovereignID"

# Instalar wget para health checks
RUN apk add --no-cache wget curl

# Crear usuario nginx si no existe
RUN id nginx > /dev/null 2>&1 || adduser -D -S -s /sbin/nologin nginx

# Borrar configuración por defecto de Nginx (solo los sitios, mantenemos el core)
RUN rm -rf /etc/nginx/conf.d/*

# Copiar nuestra configuración personalizada como un sitio de Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Crear directorios de logs con permisos correctos
RUN mkdir -p /var/log/nginx && \
    touch /var/log/nginx/access.log /var/log/nginx/error.log && \
    chown -R nginx:nginx /var/log/nginx && \
    chmod -R 755 /var/log/nginx

# ¡AQUÍ ESTÁ EL OTRO CAMBIO GUARDADO! Copiamos desde sovereignid-web
COPY --from=build --chown=nginx:nginx /app/dist/sovereignid-web/browser /usr/share/nginx/html

# Crear directorios necesarios con permisos
RUN mkdir -p /var/cache/nginx && \
    chown -R nginx:nginx /var/cache/nginx && \
    chmod -R 755 /var/cache/nginx

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]