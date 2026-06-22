## Why

El monorepo SovereignID ya define un esquema PostgreSQL completo en `database/BBDD_SovereignID.sql`, pero no existe infraestructura para levantarlo ni documentación formal de las decisiones de datos. Sin una base ejecutable, el equipo no puede explorar el modelo relacional ni validar el diseño antes de conectar microservicios. Este cambio establece el stack de desarrollo con Postgres en contenedor, dejando el script SQL como fuente de verdad del esquema.

## What Changes

- Añadir servicio `postgres` a `docker-compose.yml` con inicialización automática desde `database/BBDD_SovereignID.sql`.
- Documentar variables de entorno de conexión en `.env.example` (sin conectar servicios .NET aún).
- Añadir comandos de conveniencia en `Makefile` (`db-up`, `db-reset`, `db-logs`) para levantar y resetear la base.
- Actualizar `CONTEXT.md` con la carpeta `database/` y el flujo de exploración del esquema.
- Documentar decisiones de arquitectura de datos: SQL como fuente de verdad, Postgres compartido para MVP, sin seeds, producción en contenedor, y estrategia futura database-first con EF.

## Capabilities

### New Capabilities

- `database-dev-stack`: infraestructura local y de MVP para levantar PostgreSQL desde el script canónico, explorar el esquema y resetear el entorno de desarrollo.

### Modified Capabilities

- _(ninguna — no se modifican requisitos de auth ni otros specs existentes; `auth-api` sigue usando `InMemoryChallengeStore`)_

## Impact

- **Infraestructura**: nuevo servicio `postgres` en Docker Compose; volumen persistente para datos de desarrollo.
- **Código de aplicación**: sin cambios en `src/auth/` ni en tests de integración en esta fase.
- **Documentación**: `CONTEXT.md`, `.env.example`, `Makefile`.
- **Script SQL**: sin alteraciones al diseño del esquema; solo verificación de compatibilidad con Postgres 16+ en init.
- **Dependencias**: imagen oficial `postgres` (sin Flyway, Liquibase ni EF en esta fase).
