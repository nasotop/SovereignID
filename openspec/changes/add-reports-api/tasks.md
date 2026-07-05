## 1. Scaffold y persistencia

- [x] 1.1 Ejecutar `scaffold-microservice.ps1` para `Reports` (puertos HTTP/HTTPS + UserSecretsId) y registrar proyectos en `SovereignID.sln`
- [x] 1.2 Configurar `Reports.Infrastructure` con Postgres database-first (`efcpt-config.json`: `institutions`, `students`, `credentials`, `verification_logs`, `institution_metrics_daily`)
- [x] 1.3 Implementar `AddReportsPersistence()` con `Persistence:Provider` InMemory (tests) | Postgres (dev/prod)
- [x] 1.4 Referenciar proyecto `SovereignID.Authorization` + JWT (mismo patrón que Academy/Issuer: `AddSovereignIdAuthorizationHandlers`, `PlatformOrInstitutionMember` en rutas `{institutionId}`)

## 2. Módulo IReportCatalog (Application)

- [x] 2.1 Definir contratos de dominio: `TimeSeriesReport`, `PointInTimeReport`, `PlatformRankingReport`, enums `InstitutionReportId` / `PlatformReportId`
- [x] 2.2 Implementar `IReportCatalog` en Application con validación de período (máx. 366 días) y scope institution
- [x] 2.3 Implementar `PostgresReportCatalog` adapter: lógica híbrida snapshot + live tail por día
- [x] 2.4 Implementar joins `verification_logs` → `credentials` para reportes institution-scoped de verificaciones

## 3. Endpoints Reports.Api

- [x] 3.1 `GET /reports/institutions/{id}/credentials-issued` (R-I1)
- [x] 3.2 `GET /reports/institutions/{id}/credential-reads` (R-I2)
- [x] 3.3 `GET /reports/institutions/{id}/credentials-per-student` (R-I3)
- [x] 3.4 `GET /reports/institutions/{id}/credentials-revoked` (R-I4)
- [x] 3.5 `GET /reports/institutions/{id}/verification-outcomes` (R-I5)
- [x] 3.6 `GET /reports/platform/credentials-by-institution` (R-P1)
- [x] 3.7 `GET /reports/platform/students-by-institution` (R-P2)
- [x] 3.8 Problem Details + `ReportsFailureExceptionFilter` (códigos: `invalid_report_period`, `institution_not_found`)

## 4. Metrics snapshot job

- [x] 4.1 Implementar `MetricsSnapshotJob` (`IHostedService`) con agregación diaria documentada en design.md
- [x] 4.2 Implementar comando CLI `snapshot --from --to` para backfill Development
- [x] 4.3 Documentar en README/scripts: ejecutar backfill tras `seed-dev.ps1`

## 5. BFF pass-through

- [x] 5.1 Añadir `reports` a `scripts/openapi-lib.sh` (puerto export)
- [x] 5.2 Exportar `docs/contracts/reports.openapi.json`
- [x] 5.3 Regenerar cliente Kiota (`gen-kiota-clients.ps1`) y controllers pass-through en `Bff.Api` (JWT forward)
- [x] 5.4 Actualizar `bff.openapi.json` (reexport BFF)

## 6. Infra y documentación

- [x] 6.1 Añadir `reports-api` a `docker-compose.yml` (red interna, Postgres connection string)
- [x] 6.2 Actualizar `CONTEXT.md` con sección servicio `reports` y matriz en `docs/authorization-domain-contract.md`
- [x] 6.3 Dockerfile `Reports.Api` (si no generado por scaffold)

## 7. Tests

- [x] 7.1 `Reports.IntegrationTests`: auth forbidden/forbidden institution scope
- [x] 7.2 Tests por reporte con Postgres fixture (seed + snapshot backfill)
- [x] 7.3 Test degradación `source=live` sin filas snapshot
- [x] 7.4 Test job idempotente upsert `(institution_id, metric_date)`

## 8. CI

- [x] 8.1 Verificar `verify-openapi` incluye `reports` y `bff`
- [x] 8.2 Añadir job/test `dotnet test tests/reports/` en pipeline si aplica
