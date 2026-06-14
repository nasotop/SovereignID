# ============================================================================
# Makefile - SovereignID Docker Development
# ============================================================================
# Comandos útiles para desarrollo con Docker
# Uso: make [comando]
# ============================================================================

.PHONY: help build up down logs clean restart test validate build-prod

help:
\t@echo \"SovereignID Docker Commands\"
\t@echo \"===========================\"
\t@echo \"  make build          - Construir imágenes Docker\"
\t@echo \"  make up             - Levantar servicios (development)\"
\t@echo \"  make down           - Detener servicios\"
\t@echo \"  make logs           - Ver logs (all)\"
\t@echo \"  make logs-api       - Ver logs (auth-api)\"
\t@echo \"  make logs-web       - Ver logs (web)\"
\t@echo \"  make clean          - Limpiar contenedores y redes\"
\t@echo \"  make restart        - Reiniciar servicios\"
\t@echo \"  make validate       - Validar docker-compose.yml\"
\t@echo \"  make test           - Ejecutar tests del backend\"
\t@echo \"  make shell-api      - Acceder a shell del container auth-api\"
\t@echo \"  make shell-web      - Acceder a shell del container web\"
\t@echo \"  make build-prod     - Construir para producción\"
\t@echo \"  make push-images    - Pushear imágenes a registry\"
\t@echo \"  make status         - Ver estado de servicios\"

build:
\tdocker-compose build --no-cache

up:
\tdocker-compose up -d

down:
\tdocker-compose down

logs:
\tdocker-compose logs -f

logs-api:
\tdocker-compose logs -f auth-api

logs-web:
\tdocker-compose logs -f web

restart:
\tdocker-compose restart

clean:
\tdocker-compose down -v
\tdocker system prune -f

validate:
\tdocker-compose config

test:
\tdocker-compose exec auth-api dotnet test /src/tests/auth/Auth.IntegrationTests

shell-api:
\tdocker-compose exec auth-api /bin/sh

shell-web:
\tdocker-compose exec web /bin/sh

status:
\tdocker-compose ps

build-prod:
\tdocker-compose -f docker-compose.yml build --no-cache

# Production push (requiere registry configurado)
push-images: build-prod
\tdocker push sovereignid-auth:latest
\tdocker push sovereignid-web:latest

# Development convenience
dev-up: build up logs

dev-down: down clean

# Generar clave JWT segura
generate-jwt-key:
\t@openssl rand -base64 32
