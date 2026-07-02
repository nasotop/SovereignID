## ADDED Requirements

### Requirement: Endpoints holder autenticados por JWT SIWE

El servicio `issuer` SHALL exponer consultas de credenciales del titular autenticado:

| Método | Ruta | Propósito |
|--------|------|-----------|
| `GET` | `/issuer/holders/me/credentials` | Lista credenciales del titular |
| `GET` | `/issuer/holders/me/credentials/{credentialId}` | Detalle de credencial del titular |
| `GET` | `/issuer/credentials/{credentialId}` | Detalle autenticado con verificación de ownership |

Todos los endpoints anteriores SHALL requerir `Authorization: Bearer {jwt}` emitido por el servicio `auth` y SHALL filtrar por `credentials.subject_did = claim "did"` del JWT.

#### Scenario: Listado del titular autenticado

- **WHEN** un cliente envía `GET /issuer/holders/me/credentials` con JWT válido cuyo claim `did` coincide con `subject_did` de credenciales existentes
- **THEN** el servicio responde `200` con un arreglo de resúmenes ordenados por `issuedAt` descendente

#### Scenario: Petición sin autenticación rechazada

- **WHEN** un cliente envía `GET /issuer/holders/me/credentials` sin header `Authorization`
- **THEN** el servicio responde `401` con Problem Details y `error = unauthenticated`

#### Scenario: Detalle de credencial ajena

- **WHEN** un cliente autenticado solicita detalle de un `credentialId` cuyo `subject_did` no coincide con el claim `did` del JWT
- **THEN** el servicio responde `404` con `error = credential_not_found`

### Requirement: Contrato OpenAPI documenta autenticación Bearer

El documento OpenAPI publicado en `docs/contracts/issuer.openapi.json` SHALL declarar:

- `components.securitySchemes.bearerAuth` (tipo `http`, scheme `bearer`, bearerFormat `JWT`).
- Requisito de seguridad `bearerAuth` en las operaciones `GET` de holder y en `GET /issuer/credentials/{credentialId}`.

Los endpoints de emisión existentes (`POST` wallet, `POST` title) y `GET /health` MUST NOT requerir Bearer en el contrato OpenAPI v1 (permanecen sin `security` documentado).

#### Scenario: Snapshot incluye security scheme

- **WHEN** se exporta el OpenAPI del servicio issuer
- **THEN** el snapshot contiene `components.securitySchemes.bearerAuth`
- **AND** las operaciones holder listan `security` con `bearerAuth`

### Requirement: Status de credencial como enum en OpenAPI

El contrato OpenAPI SHALL modelar el campo `status` en `HolderCredentialSummary` y `HolderCredentialDetail` como `enum` con valores estables: `active`, `revoked`, `expired` — para que consumidores generados (p. ej. `ng-openapi-gen`) obtengan unión tipada.

#### Scenario: Codegen tipa status

- **WHEN** se genera el cliente Angular desde el snapshot issuer actualizado
- **THEN** el tipo generado de `status` es la unión `active | revoked | expired` (no `string` libre)

### Requirement: Snapshot versionado y verificable

El contrato HTTP del issuer SHALL mantenerse en `docs/contracts/issuer.openapi.json` (sin bloque `servers`) y CI SHALL ejecutar `verify-openapi` para detectar drift respecto al servicio vivo.

Regeneración: `bash scripts/export-openapi.sh issuer` (requiere servicio issuer arrancable).

#### Scenario: CI detecta snapshot desactualizado

- **WHEN** el OpenAPI vivo difiere del snapshot commiteado
- **THEN** el job `verify-openapi` falla

### Requirement: Proyección de lectura holder

Las respuestas de listado SHALL incluir por credencial: `id`, `title` (nombre del tipo), `issuerName`, `typeCode`, `status`, `issuedAt`, `expiresAt` (nullable).

El detalle SHALL extender el resumen con: `subjectDid`, `issuer` (`did`, `displayName`, `code`), `anchors` (IPFS/on-chain), `metadata` (nullable), `revokedAt` (nullable).

#### Scenario: Resumen alimenta dashboard

- **WHEN** el backend responde listado holder
- **THEN** cada elemento incluye los campos mínimos para renderizar una tarjeta del portal web sin joins adicionales
