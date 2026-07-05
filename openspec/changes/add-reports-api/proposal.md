## Why

Los portales **platform** e **institution** (admin/issuer) necesitan dashboards con KPIs operativos (emisiones, verificaciones, alumnos), pero hoy no existe ningún servicio de lectura analítica: la BD ya modela fuentes (`credentials`, `verification_logs`, `students`, `institution_metrics_daily`) y un job nocturno documentado, pero ningún microservicio las expone de forma segura por perfil JWT.

Este cambio introduce **`reports-api`** como microservicio de solo lectura, con contrato OpenAPI versionado y pass-through en el BFF. Alcance v1: **backend únicamente** (sin UI); REST explícito por reporte (sin GraphQL).

## What Changes

- Nuevo microservicio `src/reports/` (`Reports.Domain`, `Reports.Application`, `Reports.Infrastructure`, `Reports.Api`) con persistencia Postgres read-only (database-first, mismo patrón ADR-0002).
- Endpoints REST fijos para **7 reportes** (5 institution + 2 platform ampliados con desglose complementario), parametrizados por período (`from`, `to`) y granularidad diaria donde aplique.
- Autorización JWT reutilizando la librería compartida `SovereignID.Authorization` (mismo patrón que `academy-api` / `issuer-api`): institution reports usan policy `PlatformOrInstitutionMember` (`admin`, `issuer`, **`viewer`**) con `InstitutionRouteAuthorizationHandler`; platform reports exigen `PlatformAdmin` (claim desde `user_global_roles` o allowlist legacy en auth).
- Job nocturno v1 mínimo (o script manual documentado) que puebla `institution_metrics_daily` — prerequisito para reportes marcados como **snapshot**; reportes **live** consultan tablas operativas directamente.
- Snapshot OpenAPI `docs/contracts/reports.openapi.json`, registro en `scripts/openapi-lib.sh`, cliente Kiota en BFF y controllers pass-through (JWT reenviado, sin agregación en BFF).
- Servicio `reports-api` en `docker-compose.yml` (red interna; expuesto al browser solo vía `/api/reports/…`).

## Capabilities

### New Capabilities

- `reports-institution-dashboard`: reportes de lectura scoped a una institución para perfiles `admin`, `issuer` y `viewer` (emisiones, verificaciones/leídas, promedio credenciales/alumno, revocaciones, desglose verificaciones válidas/inválidas). Consumidor UI previsto: portal `/academy` (y métricas operativas en `/issuer` en cambio posterior).
- `reports-platform-dashboard`: reportes cross-tenant para `platform_admin` (credenciales por institución, alumnos por institución, ranking/resumen verificaciones por institución).
- `reports-metrics-snapshot-job`: población idempotente de `institution_metrics_daily` desde tablas operativas (job nocturno o comando batch v1).

### Modified Capabilities

- _(ninguna — el BFF gana rutas pass-through pero no cambia requisitos de auth, issuer, academy ni verifier)_

## Impact

- **Nuevo código**: `src/reports/**`, tests `tests/reports/Reports.IntegrationTests/`.
- **BFF**: controllers pass-through, Kiota generado, entrada en `openapi-lib.sh` y `gen-kiota-clients.ps1`.
- **Infra**: `docker-compose.yml`, `.env.example`, posible entrada en `SovereignID.sln`.
- **BD**: sin cambio de esquema en v1; lecturas sobre tablas existentes. El job escribe en `institution_metrics_daily`.
- **Frontend**: fuera de alcance en este cambio (contrato quedará listo para consumo posterior vía BFF).
- **Documentación**: `CONTEXT.md` (sección servicio reports), fila en `docs/authorization-domain-contract.md` (matriz MVP), posible ADR si el job vive en reports-api vs. worker separado.
