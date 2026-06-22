## Context

El monorepo SovereignID contiene un script SQL canónico en `database/BBDD_SovereignID.sql` que define el esquema completo del MVP: tipos ENUM, 12 tablas (instituciones, usuarios, estudiantes, credenciales, verificación, auditoría, métricas, auth challenges), índices, comentarios y claves foráneas. Hoy no hay servicio PostgreSQL en `docker-compose.yml` ni documentación del flujo de datos en `CONTEXT.md`.

El microservicio `auth-api` persiste retos SIWE en memoria (`InMemoryChallengeStore`). La tabla `auth_challenges` existe en el script como diseño futuro para despliegue multi-instancia, pero **no se conectará en este cambio**.

Decisiones acordadas con el equipo:
- Objetivo inmediato: levantar BD para explorar el esquema.
- Fuente de verdad del esquema: el archivo `.sql`; en el futuro EF será database-first (scaffold), no code-first.
- Arquitectura de datos: un Postgres compartido para todo el MVP.
- Sin datos seed en esta fase.
- Producción MVP: Postgres en contenedor (no managed cloud).

## Goals / Non-Goals

**Goals:**

- Levantar PostgreSQL en Docker con el esquema aplicado automáticamente al primer arranque.
- Permitir explorar el modelo con herramientas estándar (`psql`, DBeaver, pgAdmin).
- Documentar decisiones de arquitectura de datos y el flujo de reset para desarrollo.
- Dejar preparado el contrato de variables de entorno para futura conexión de microservicios.

**Non-Goals:**

- Conectar `auth-api` u otros servicios .NET a la base de datos.
- Añadir Entity Framework Core, Flyway o Liquibase.
- Incluir datos seed (instituciones, usuarios, tipos de credencial).
- Modificar el contenido del esquema en `BBDD_SovereignID.sql`.
- Configurar backups automatizados o alta disponibilidad.

## Decisions

### 1. Inicialización vía `docker-entrypoint-initdb.d`

**Decisión:** Montar `database/BBDD_SovereignID.sql` en `/docker-entrypoint-initdb.d/01-schema.sql` del contenedor Postgres.

**Alternativas consideradas:**
- *Ejecución manual con `psql`* — descartada: no reproducible entre devs ni CI.
- *Flyway/Liquibase* — descartada para esta fase: el esquema es estático y solo se explora; añade tooling prematuro.
- *EF Core migrations* — descartada: contradice la decisión de SQL como fuente de verdad.

**Rationale:** El mecanismo nativo de la imagen oficial Postgres ejecuta scripts `.sql` en orden alfabético solo cuando el volumen de datos está vacío. Es el camino de menor fricción para bootstrap.

### 2. Imagen `postgres:16-alpine`

**Decisión:** Usar Postgres 16 sobre Alpine.

**Rationale:** El script usa `gen_random_uuid()` (disponible nativamente desde PG 13), `jsonb`, `inet`, ENUMs e `IDENTITY`. PG 16 es LTS reciente y estable. Alpine reduce tamaño de imagen.

### 3. Base de datos creada por variable de entorno, no en el script

**Decisión:** Configurar `POSTGRES_DB=sovereignid` en el servicio Docker; el script SQL **no** incluirá `CREATE DATABASE`.

**Rationale:** El entrypoint de Postgres crea la base antes de ejecutar init scripts. Evita duplicar lógica y errores de conexión.

### 4. Postgres compartido en red `sovereign-net`

**Decisión:** Un único servicio `postgres` accesible por todos los microservicios futuros en la misma red Docker.

**Alternativas consideradas:**
- *BD por microservicio* — descartada para MVP: el script actual es un esquema unificado; partirlo sería trabajo futuro innecesario ahora.

### 5. Volumen nombrado con reset explícito

**Decisión:** Volumen Docker `postgres_data` persistente; comando `make db-reset` ejecuta `docker compose down -v` del volumen de postgres y `up` para re-aplicar el script.

**Rationale:** El init de Postgres no es idempotente (no hay `IF NOT EXISTS` en tipos/tablas). Resetear requiere borrar el volumen. Documentar esto evita confusión al iterar el script.

### 6. Sin dependencia de `auth-api` hacia postgres

**Decisión:** `auth-api` no declara `depends_on: postgres` en esta fase.

**Rationale:** Auth funciona sin BD; acoplar servicios ahora añade ruido al healthcheck y al arranque.

### 7. Estrategia futura: database-first con EF

**Decisión documentada (no implementada):** Cuando un microservicio necesite persistencia, el flujo será:
1. Actualizar `database/BBDD_SovereignID.sql` (o migraciones versionadas cuando el esquema evolucione).
2. Regenerar DbContext con `dotnet ef dbcontext scaffold`.
3. El código .NET consume el esquema; no lo define.

**Alternativa descartada:** Code-first EF como fuente de verdad — generaría drift respecto al script canónico.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| Init script solo corre una vez por volumen | Documentar `make db-reset`; no usar `down -v` en producción |
| Script no idempotente | Aceptable para bootstrap; Flyway cuando el esquema empiece a cambiar con frecuencia |
| Credenciales en `.env.example` son de desarrollo | Documentar que producción usa Docker secrets; no commitear `.env` real |
| Puerto 5432 puede colisionar con Postgres local | Exponer en host vía variable `POSTGRES_PORT` configurable (default 5432) |
| Sin seeds, tablas de catálogo vacías | Aceptado por decisión de producto; exploración de esquema no requiere datos |

## Migration Plan

1. Añadir servicio `postgres` a `docker-compose.yml`.
2. Actualizar `.env.example` con variables de Postgres.
3. Añadir targets `db-up`, `db-reset`, `db-logs`, `db-shell` al `Makefile`.
4. Actualizar `CONTEXT.md` con sección de base de datos.
5. Validar: `make db-reset` → conectar con `psql` → verificar tablas y enums.

**Rollback:** Eliminar servicio postgres de compose y volumen; sin impacto en `auth-api` ni `web`.

## Open Questions

- _(ninguna bloqueante para esta fase)_
- *Futuro:* ¿Cuándo introducir Flyway para migraciones incrementales? (Cuando el esquema deje de ser estático o haya múltiples devs modificándolo en paralelo.)
- *Futuro:* ¿Primer servicio en conectar persistencia — `auth_challenges` o dominio de credenciales?
