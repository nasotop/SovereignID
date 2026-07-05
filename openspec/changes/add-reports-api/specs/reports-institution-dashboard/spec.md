## ADDED Requirements

### Requirement: Credenciales emitidas en período (institution)

El servicio reports SHALL exponer `GET /reports/institutions/{institutionId}/credentials-issued` con query params obligatorios `from` y `to` (fecha ISO `YYYY-MM-DD`, inclusive).

La respuesta SHALL incluir `period`, `total`, `series[]` con `{ date, value }` y `source` ∈ { `snapshot`, `live`, `hybrid` }.

Para días históricos completos, el servicio SHALL preferir `institution_metrics_daily.credentials_issued`. Para el día corriente o fechas sin fila snapshot, SHALL completar con conteo live sobre `credentials.issued_at` scoped a `institutionId`.

Solo usuarios con policy `PlatformOrInstitutionMember` (`admin`, `issuer` o `viewer` en `{institutionId}`) o platform admin SHALL acceder.

#### Scenario: Issuer obtiene serie de emisiones últimos 30 días

- **WHEN** un JWT con membership `{institutionId}:issuer` llama con `from` hace 30 días y `to` hoy
- **THEN** responde `200` con `total` igual a la suma de la serie y `source` `hybrid` si el día actual no está snapshoteado

#### Scenario: Viewer institucional accede en solo lectura

- **WHEN** un JWT con membership `{institutionId}:viewer` llama al endpoint
- **THEN** responde `200` con la misma forma de datos que admin/issuer

#### Scenario: Usuario sin membership recibe forbidden

- **WHEN** un JWT sin membership en `{institutionId}` ni platform admin llama al endpoint
- **THEN** responde `403` con Problem Details

### Requirement: Credenciales leídas (verificaciones) en período (institution)

El servicio reports SHALL exponer `GET /reports/institutions/{institutionId}/credential-reads` con `from` y `to`.

“Leídas” SHALL contar filas en `verification_logs` cuya credencial pertenece a `{institutionId}` (join vía `credential_id` → `credentials.institution_id`), agrupadas por día de `verified_at`.

Histórico SHALL usar `institution_metrics_daily.verifications_total` cuando exista; tail live para huecos y día corriente.

#### Scenario: Admin institucional ve total de verificaciones del mes

- **WHEN** un JWT `{institutionId}:admin` consulta el mes calendario completo ya snapshoteado
- **THEN** responde `200` con `source` `snapshot` y `total` consistente con la suma diaria del snapshot

### Requirement: Promedio de credenciales por alumno (institution)

El servicio reports SHALL exponer `GET /reports/institutions/{institutionId}/credentials-per-student` con query param obligatorio `asOf` (fecha).

SHALL calcular en vivo:
- `totalCredentials` = credenciales con `issued_at <= asOf` (todas las filas, cualquier status salvo borrado lógico inexistente).
- `totalStudents` = estudiantes con `is_active = true` y `created_at <= asOf`.
- `averageCredentialsPerStudent` = `totalCredentials / totalStudents`, `0` si `totalStudents = 0`.

SHALL NOT usar `institution_metrics_daily.unique_students_active` para este reporte.

#### Scenario: Promedio con alumnos registrados

- **WHEN** la institución tiene 10 alumnos activos y 15 credenciales emitidas hasta `asOf`
- **THEN** responde `averageCredentialsPerStudent` = `1.5` y `source` implícito live (campo `asOf` presente)

### Requirement: Credenciales revocadas en período (institution)

El servicio reports SHALL exponer `GET /reports/institutions/{institutionId}/credentials-revoked` con `from` y `to`.

SHALL usar `institution_metrics_daily.credentials_revoked` para histórico y conteo live por `credentials.revoked_at` para tail/huecos.

#### Scenario: Serie incluye revocaciones del período

- **WHEN** existen 2 revocaciones snapshoteadas y 1 revocación hoy no snapshoteada
- **THEN** `total` es 3 y `source` es `hybrid`

### Requirement: Desglose verificaciones válidas vs inválidas (institution)

El servicio reports SHALL exponer `GET /reports/institutions/{institutionId}/verification-outcomes` con `from` y `to`.

La respuesta SHALL incluir `validTotal`, `invalidTotal`, y opcionalmente `series[]` con `{ date, valid, invalid }`.

Histórico SHALL usar columnas `verifications_valid` e `verifications_invalid` del snapshot; tail live clasifica `verification_logs.result`.

#### Scenario: Outcomes del período

- **WHEN** el issuer consulta una semana con snapshots completos
- **THEN** `validTotal + invalidTotal` equals el total de verificaciones institution-scoped del período según snapshot

### Requirement: Límite de período

Endpoints con `from`/`to` SHALL rechazar rangos mayores a 366 días con `400` y Problem Details `error = invalid_report_period`.

#### Scenario: Período demasiado largo

- **WHEN** `from` y `to` abarcan más de 366 días
- **THEN** responde `400` con código estable `invalid_report_period`
