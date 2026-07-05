## ADDED Requirements

### Requirement: Job nocturno de métricas diarias

El servicio reports SHALL incluir un componente `MetricsSnapshotJob` que, al menos una vez por día, calcule y upsert filas en `institution_metrics_daily` para el día anterior (UTC).

Por cada par `(institution_id, metric_date)` SHALL persistir:
- `credentials_issued`, `credentials_revoked`
- `verifications_total`, `verifications_valid`, `verifications_invalid`
- `unique_students_active` (estudiantes distintos con emisión o verificación ese día)
- `computed_at` = timestamp de ejecución

El job SHALL ser idempotente: re-ejecutar el mismo día sobrescribe valores, no duplica filas.

#### Scenario: Job procesa día anterior

- **WHEN** el job corre tras medianoche UTC
- **THEN** existe una fila por institución con `metric_date` = ayer y contadores consistentes con tablas operativas

### Requirement: Backfill manual para desarrollo

El servicio reports SHALL exponer un comando CLI documentado (p. ej. argumento `snapshot --from --to`) ejecutable en Development para poblar rango histórico de snapshots sin esperar al scheduler.

#### Scenario: Backfill en entorno local

- **WHEN** el desarrollador ejecuta backfill para los últimos 7 días tras `seed-dev`
- **THEN** `institution_metrics_daily` contiene 7 filas por institución seed con datos coherentes

### Requirement: Reportes degradan sin snapshot

Los endpoints de serie temporal SHALL seguir funcionando si `institution_metrics_daily` está vacío, usando únicamente queries live y devolviendo `source = live`.

#### Scenario: Entorno sin job previo

- **WHEN** no hay filas snapshot para el período solicitado
- **THEN** el endpoint responde `200` con datos live y `source` `live`
