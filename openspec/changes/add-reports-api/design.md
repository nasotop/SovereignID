## Context

SovereignID es un monorepo de microservicios (.NET 10 + Angular) con Postgres compartido (`database/BBDD_SovereignID.sql` como fuente de verdad). Tras los merges recientes en `feat/reporteria` (dev #17–#21):

| Cambio relevante | Impacto en reportería |
|------------------|----------------------|
| RBAC formalizado (`docs/authorization-domain-contract.md`) | Matriz de permisos; nuevo rol `viewer` (solo lectura institucional) |
| `user_global_roles` + patch `2026-07-04-academy-authz.sql` | `platform_admin` resuelto en auth desde BD (además de allowlist legacy) |
| Librería `SovereignID.Authorization` + `InstitutionRouteAuthorizationHandler` | reports-api reutiliza policies/handlers; no reinventar scope |
| Portal `/academy` (admin, issuer, viewer) y `/platform` con layout KPI | Consumidores UI naturales de reportes institution vs platform |
| Academy ampliado (listados estudiantes/usuarios) | **No sustituye** reportes agregados cross-tenant ni series temporales |

Perfiles JWT relevantes para reportería:

| Perfil | Claim / policy | Scope | Portal UI (futuro) |
|--------|----------------|-------|-------------------|
| Platform admin | `platform_admin=true` | Cross-tenant | `/platform` |
| Institution admin / issuer / **viewer** | `membership` `{institutionId}:{role}` | Una institución | `/academy` (viewer incluido) |

La tabla `institution_metrics_daily` documenta snapshots diarios calculados por **job nocturno**, pero **ningún job existe aún** en código. Fuentes operativas: `credentials`, `verification_logs` (join vía `credential_id`), `students`, `institutions`. Tablas nuevas (`holder_profiles`, `user_global_roles`) **no participan** en reportes v1.

Decisión de producto ya tomada:
- **Nuevo microservicio `reports-api`** (no módulo en BFF).
- **BFF pass-through** con JWT reenviado (patrón ADR-0005).
- **REST explícito**, sin GraphQL.
- **v1 backend only** — sin portales Angular.

## Goals / Non-Goals

**Goals:**
- Exponer 7 reportes fijos con contrato OpenAPI versionado y autorización por perfil.
- Clasificar y documentar fuente de datos (snapshot vs live) por reporte.
- Implementar job v1 que puebla `institution_metrics_daily` para desacoplar dashboards de scans costosos.
- Módulo profundo `IReportCatalog` en Application: poca interfaz, lógica de scope + agregación concentrada.

**Non-Goals:**
- UI / dashboards Angular (cambio posterior).
- GraphQL, BI ad-hoc, export CSV/PDF.
- Reportes holder o verifier público.
- Nuevo esquema SQL (vistas materializadas opcionales en v2).
- Agregación o composición en BFF.

## Catálogo de reportes y mockups

### Portal institution (`/academy` — admin, issuer y viewer)

Layout propuesto (wireframe):

```
┌──────────────────────────────────────────────────────────────────┐
│  Institution Dashboard — Duoc UC          [periodo: últimos 30d] │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────┐ │
│  │ Emitidas    │  │ Leídas      │  │ Revocadas   │  │ Prom/alu │ │
│  │    127      │  │    842      │  │      3      │  │   1.4    │ │
│  │  ▁▂▃▅▇ chart│  │  ▃▄▅▆ chart │  │  ▁▁▂ chart  │  │  (live)  │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └──────────┘ │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Verificaciones: válidas ████████░░ 712  |  inválidas ██░ 130   │ │
│  └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

| ID | Reporte | Endpoint v1 | Fuente | Clasificación |
|----|---------|-------------|--------|---------------|
| **R-I1** | Credenciales emitidas en período | `GET /reports/institutions/{id}/credentials-issued?from&to` | `institution_metrics_daily.credentials_issued` (serie diaria); cola del período incluyendo **hoy** vía query live sobre `credentials.issued_at` si el snapshot del día no existe | **Snapshot + live tail** |
| **R-I2** | Credenciales leídas en período | `GET /reports/institutions/{id}/credential-reads?from&to` | `institution_metrics_daily.verifications_total`; tail live: `verification_logs` ⋈ `credentials` por `verified_at` | **Snapshot + live tail** |
| **R-I3** | Promedio credenciales por alumno | `GET /reports/institutions/{id}/credentials-per-student?asOf` | `COUNT(credentials WHERE issued_at <= asOf) / COUNT(students WHERE is_active AND created_at <= asOf)` — denominador = alumnos activos registrados, no `unique_students_active` del snapshot | **Live only** |
| **R-I4** | Credenciales revocadas en período | `GET /reports/institutions/{id}/credentials-revoked?from&to` | `institution_metrics_daily.credentials_revoked`; tail live por `credentials.revoked_at` | **Snapshot + live tail** |
| **R-I5** | Verificaciones válidas vs inválidas | `GET /reports/institutions/{id}/verification-outcomes?from&to` | Snapshot: `verifications_valid` / `verifications_invalid`; tail live: `verification_logs.result IN ('valid')` vs resto scoped por institución | **Snapshot + live tail** |

**Nota semántica R-I2:** “leídas” = intentos de verificación registrados en `verification_logs` para credenciales de la institución (incluye válidas e inválidas). No equivale a “descargas” de IPFS.

**Nota R-I3:** `unique_students_active` del snapshot mide actividad diaria (alumnos con emisión o verificación ese día), **no** sirve como denominador del promedio. Por eso este reporte es live.

### Portal platform (`/platform` — platform admin)

```
┌──────────────────────────────────────────────────────────────────┐
│  Platform Overview                    [periodo: últimos 30d]      │
├──────────────────────────────────────────────────────────────────┤
│  Credenciales por institución (bar chart)                         │
│  Duoc UC     ████████████████████ 127                           │
│  U. Chile    ████████████ 84                                      │
│  ...                                                              │
├──────────────────────────────────────────────────────────────────┤
│  Alumnos por institución (bar chart, estado actual)               │
│  Duoc UC     █████████████████████████ 312                        │
│  U. Chile    ██████████████ 178                                   │
└──────────────────────────────────────────────────────────────────┘
```

| ID | Reporte | Endpoint v1 | Fuente | Clasificación |
|----|---------|-------------|--------|---------------|
| **R-P1** | Credenciales por institución | `GET /reports/platform/credentials-by-institution?from&to` | `SUM(institution_metrics_daily.credentials_issued)` GROUP BY `institution_id` + tail live; enriquecer con `institutions.display_name` | **Snapshot + live tail** |
| **R-P2** | Alumnos por institución | `GET /reports/platform/students-by-institution?asOf` | `COUNT(students)` WHERE `is_active` AND `created_at <= asOf` GROUP BY `institution_id` | **Live only** |

No existe columna de “total alumnos” en `institution_metrics_daily`; `unique_students_active` es métrica diaria de actividad, no stock.

### Forma de respuesta común (serie temporal)

Reportes R-I1, R-I2, R-I4, R-I5, R-P1 devuelven:

```json
{
  "period": { "from": "2026-03-01", "to": "2026-03-31" },
  "source": "hybrid",
  "total": 127,
  "series": [{ "date": "2026-03-01", "value": 4 }]
}
```

Reportes R-I3, R-P2 devuelven snapshot puntual (`asOf`):

```json
{
  "asOf": "2026-03-31",
  "totalStudents": 120,
  "totalCredentials": 168,
  "averageCredentialsPerStudent": 1.4
}
```

R-P1 lista instituciones:

```json
{
  "period": { "from": "...", "to": "..." },
  "items": [
    { "institutionId": "...", "displayName": "Duoc UC", "total": 127 }
  ]
}
```

## Arquitectura

```
 Browser ──▶ nginx (/api/) ──▶ bff-api (pass-through, JWT forward)
                                    │
                                    ▼
                              reports-api
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
            IReportCatalog                   MetricsSnapshotJob
         (Application seam)                  (hosted service / CLI)
                    │                               │
                    ▼                               ▼
         PostgresReportReader              writes institution_metrics_daily
         (Infrastructure adapter)          reads credentials + verification_logs
                    │
                    ▼
              PostgreSQL (shared)
```

### Módulo profundo: `IReportCatalog`

Interfaz pequeña en `Reports.Application`:

```csharp
Task<TimeSeriesReport> GetInstitutionTimeSeriesAsync(
    InstitutionReportId reportId, Guid institutionId, DateOnly from, DateOnly to, ...);

Task<PointInTimeReport> GetInstitutionPointInTimeAsync(
    InstitutionReportId reportId, Guid institutionId, DateOnly asOf, ...);

Task<PlatformInstitutionRanking> GetPlatformRankingAsync(
    PlatformReportId reportId, DateOnly from, DateOnly to, ...);
```

La implementación concentra:
- Resolución snapshot vs live tail por día.
- Join `verification_logs` → `credentials` para scope institucional.
- Validación de que el caller tiene membership (delegada a authorization handler + comprobación en use case).
- Límites de período (p. ej. máx. 366 días).

**Deletion test:** si se elimina `IReportCatalog`, la lógica de híbrido snapshot/live y joins reaparece en cada controller.

## Decisions

### D1: Microservicio `reports-api` dedicado (no BFF.Application)

El BFF solo reenvía; la profundidad vive en reports. Alternativa rechazada: módulo en BFF — mezclaría agregación analítica con pass-through y acoplaría lecturas pesadas al hop del front.

### D2: REST con un endpoint por reporte (no catálogo genérico HTTP)

Siete rutas explícitas mantienen OpenAPI tipado y CI `verify-openapi` predecible. Alternativa rechazada: `POST /reports/execute { reportId }` — misma profundidad interna pero peor ergonomía de contrato y codegen.

### D3: Híbrido snapshot + live tail para series temporales

Días completos en `[from, yesterday]` leen `institution_metrics_daily`; el día corriente (y huecos sin snapshot) se completan con query live. Alternativa rechazada: solo live en v1 — simplifica job pero penaliza issuer con scans sobre `verification_logs` en cada dashboard load.

### D4: Job de snapshot dentro de `reports-api` como `IHostedService` + comando manual

Un `MetricsSnapshotJob` corre nightly (configurable cron) y puede invocarse con `dotnet run --project Reports.Api -- snapshot --date 2026-03-01` para backfill dev. Alternativa diferida: worker separado — overkill para MVP.

**Reglas de agregación diaria** (por `institution_id`, `metric_date`):

| Columna snapshot | Regla SQL |
|------------------|-----------|
| `credentials_issued` | `COUNT(*)` FROM `credentials` WHERE `issued_at::date = metric_date` |
| `credentials_revoked` | `COUNT(*)` WHERE `revoked_at::date = metric_date` |
| `verifications_total` | `COUNT(vl.*)` FROM `verification_logs vl` JOIN `credentials c` ON `vl.credential_id = c.id` WHERE `vl.verified_at::date = metric_date` |
| `verifications_valid` | Igual, `vl.result = 'valid'` |
| `verifications_invalid` | Igual, `vl.result <> 'valid'` AND `credential_id IS NOT NULL` |
| `unique_students_active` | `COUNT(DISTINCT c.student_id)` desde emisiones ese día ∪ estudiantes con verificación ese día |

Upsert idempotente por `(institution_id, metric_date)`.

### D5: Autorización — librería `SovereignID.Authorization`

| Ruta | Policy | Roles institution en ruta |
|------|--------|---------------------------|
| `/reports/institutions/{institutionId}/*` | `PlatformOrInstitutionMember` | `admin`, `issuer`, **`viewer`** |
| `/reports/platform/*` | `PlatformAdmin` | — |

`InstitutionRouteAuthorizationHandler` resuelve `{institutionId}` desde la ruta (mismo mecanismo que academy). Platform admin puede leer reportes institution de cualquier id.

Actualizar `docs/authorization-domain-contract.md` con fila “Ver reportes/dashboard” = Sí para `platform_admin`, `admin`, `issuer`, `viewer`.

### D6: Persistencia database-first read-only

Scaffold EF desde tablas existentes (`efcpt-config.json` incluye `institution_metrics_daily`, `credentials`, `verification_logs`, `students`, `institutions`). Sin migraciones Code-First (ADR-0002).

### D7: Sin GraphQL

Composición de cliente no es suficientemente variable para justificar segundo stack de contratos.

## Risks / Trade-offs

- [Tabla snapshot vacía al inicio] → Documentar `snapshot` CLI en seed-dev flow; integration tests insertan filas fixture.
- [`verification_logs` crece rápido; joins costosos en live tail] → Tail limitado a “hoy”; histórico solo snapshot; índice existente `idx_cred_verified_at` ayuda.
- [Semántica “promedio” ambigua] → Spec fija denominador = alumnos activos registrados; documentar en OpenAPI `description`.
- [Drift snapshot vs live] → Campo `source` en respuesta (`snapshot`, `live`, `hybrid`) para debug; job idempotente.
- [Nuevo servicio en compose + openapi registry] → Seguir checklist de `scaffold-microservice.ps1` y `openapi-lib.sh`.

## Migration Plan

1. Scaffold `reports-api`, JWT, policies, Postgres reader, endpoints R-I1…R-P2.
2. Implementar `MetricsSnapshotJob` + comando backfill; ejecutar en dev tras seed.
3. Exportar `docs/contracts/reports.openapi.json`; Kiota + BFF pass-through.
4. Integration tests por reporte (InMemory fake reader + Postgres opcional).
5. `docker compose` + documentar en CONTEXT.md.

Rollback: quitar servicio de compose y rutas BFF; no altera esquema.

## Open Questions

- ¿Incluir filtro por `credential_type_id` en v2 (desglose por tipo TITULO/NOTAS)?
- ¿Rate limiting en platform ranking (muchas instituciones)?
- ¿Timezone para cortes diarios — UTC vs `America/Santiago`? **Propuesta v1:** UTC en backend; UI formatea.
