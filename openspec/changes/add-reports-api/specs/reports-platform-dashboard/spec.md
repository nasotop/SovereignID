## ADDED Requirements

### Requirement: Credenciales por institución (platform)

El servicio reports SHALL exponer `GET /reports/platform/credentials-by-institution` con `from` y `to`.

Solo platform admin SHALL acceder (policy `PlatformAdmin`).

La respuesta SHALL incluir `period`, `items[]` con `{ institutionId, displayName, total }` ordenados por `total` descendente.

`total` por institución SHALL ser la suma de emisiones en el período, usando snapshot histórico + live tail con la misma regla que R-I1.

#### Scenario: Platform admin ve ranking de emisiones

- **WHEN** un JWT platform admin consulta el último trimestre
- **THEN** responde `200` con un item por institución activa que tenga emisiones o cero explícito

#### Scenario: Issuer institucional no accede a ranking platform

- **WHEN** un JWT solo con membership issuer llama al endpoint platform
- **THEN** responde `403` con Problem Details

### Requirement: Alumnos por institución (platform)

El servicio reports SHALL exponer `GET /reports/platform/students-by-institution` con query param obligatorio `asOf`.

Solo platform admin SHALL acceder.

SHALL devolver `items[]` con `{ institutionId, displayName, totalStudents }` contando en vivo estudiantes activos (`students.is_active = true`, `created_at <= asOf`).

#### Scenario: Conteo de alumnos activos cross-tenant

- **WHEN** platform admin consulta `asOf` = hoy
- **THEN** cada item refleja el stock actual de alumnos activos por institución, no métricas diarias de actividad

### Requirement: Contrato HTTP versionado

El contrato OpenAPI del servicio reports SHALL publicarse en `docs/contracts/reports.openapi.json` (sin bloque `servers`) y CI SHALL incluir `reports` en `verify-openapi`.

#### Scenario: Snapshot desactualizado falla CI

- **WHEN** el OpenAPI vivo difiere del snapshot commiteado
- **THEN** el job `verify-openapi` falla
