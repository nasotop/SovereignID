## 1. Docker Compose — servicio Postgres

- [ ] 1.1 Añadir servicio `postgres` a `docker-compose.yml` con imagen `postgres:16-alpine`, red `sovereign-net` y volumen nombrado `postgres_data`
- [ ] 1.2 Configurar variables `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` desde `.env` con valores por defecto documentados
- [ ] 1.3 Exponer puerto `${POSTGRES_PORT:-5432}:5432` al host
- [ ] 1.4 Montar `database/BBDD_SovereignID.sql` en `/docker-entrypoint-initdb.d/01-schema.sql` (solo lectura)
- [ ] 1.5 Añadir healthcheck con `pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}`
- [ ] 1.6 Verificar que `auth-api` y `web` NO declaran `depends_on: postgres`

## 2. Variables de entorno

- [ ] 2.1 Añadir sección PostgreSQL a `.env.example` con `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`
- [ ] 2.2 Documentar plantilla de connection string para futuros microservicios (ej. `Host=localhost;Port=5432;Database=sovereignid;Username=...;Password=...`)

## 3. Makefile — comandos de conveniencia

- [ ] 3.1 Añadir target `db-up` que levanta solo el servicio `postgres`
- [ ] 3.2 Añadir target `db-reset` que ejecuta `docker compose down` eliminando el volumen de postgres y vuelve a levantar
- [ ] 3.3 Añadir target `db-logs` para ver logs del contenedor postgres
- [ ] 3.4 Añadir target `db-shell` que abre `psql` interactivo dentro del contenedor
- [ ] 3.5 Actualizar target `help` con los nuevos comandos

## 4. Documentación

- [ ] 4.1 Actualizar mapa del monorepo en `CONTEXT.md` incluyendo carpeta `database/` y servicio postgres
- [ ] 4.2 Documentar flujo de exploración: `make db-up` → conectar con psql/DBeaver → consultas útiles
- [ ] 4.3 Documentar decisión de SQL como fuente de verdad y estrategia futura database-first con EF
- [ ] 4.4 Documentar que `make db-reset` destruye datos y re-aplica el script (solo desarrollo)

## 5. Validación manual

- [ ] 5.1 Ejecutar `make db-reset` y confirmar que el contenedor postgres arranca healthy
- [ ] 5.2 Conectar con `psql` y verificar existencia de las 12 tablas del script
- [ ] 5.3 Verificar ENUM types (`user_role`, `credential_status`, `verification_result`, `wallet_status`)
- [ ] 5.4 Verificar que tablas están vacías (sin seed data)
- [ ] 5.5 Confirmar que `auth-api` sigue funcionando sin postgres (`GET /auth/nonce` → 200)
- [ ] 5.6 Ejecutar `docker compose config` para validar sintaxis del compose actualizado
