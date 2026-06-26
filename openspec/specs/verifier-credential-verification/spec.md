# verifier-credential-verification Specification

## Purpose

Define el contrato de dominio del servicio `verifier`: la verificación de una Verifiable Credential por UUID mediante `POST /verifications`, el veredicto escalonado v1, las reglas de precedencia, el registro de cada intento y el manejo de errores de entrada como Problem Details.

## Requirements

### Requirement: Verificación de credencial por UUID

El servicio `verifier` SHALL exponer `POST /verifications` que acepta un cuerpo JSON `{ "credentialId": "<uuid>" }` y devuelve un veredicto de verificación de la Verifiable Credential identificada por ese UUID (`credentials.id`).

El endpoint SHALL ser público (anónimo): no requiere bearer token ni autenticación.

La entrada SHALL ser el UUID de la credencial; el servicio resuelve la fila `credentials` y de ella los anchors (CID, hashes, tx). El servicio MUST NOT aceptar el IPFS CID ni una VC completa como entrada en v1.

#### Scenario: Credencial válida

- **WHEN** se hace `POST /verifications` con `credentialId` de una credencial existente, no revocada y no expirada
- **THEN** la respuesta es `200 OK`
- **AND** `result` es `valid`
- **AND** `checks.found`, `checks.notRevoked` y `checks.notExpired` son `true`
- **AND** el bloque `credential` incluye `type`, `status`, `issuedAt`, `expiresAt`, `subjectDid`, `issuer` y `anchors`

#### Scenario: Endpoint accesible sin autenticación

- **WHEN** se hace `POST /verifications` sin cabecera `Authorization`
- **THEN** la solicitud se procesa normalmente (no `401`)

### Requirement: Veredicto escalonado v1

El veredicto v1 SHALL computar únicamente los chequeos sin dependencia externa: `found`, `notRevoked`, `notExpired`.

Los chequeos con dependencia externa (`hashMatches`, `onChainExists`, `signatureValid`) MUST devolverse como `null` ("no evaluado") en v1.

Los valores de `result` emitidos en v1 SHALL ser exactamente: `valid`, `revoked`, `expired`, `not_found`. Los valores `invalid_signature`, `tampered` e `ipfs_unreachable` quedan reservados en el contrato y MUST NOT emitirse en v1.

#### Scenario: Chequeos externos no evaluados

- **WHEN** cualquier verificación se ejecuta en v1
- **THEN** `checks.hashMatches`, `checks.onChainExists` y `checks.signatureValid` son `null`

### Requirement: Precedencia y cómputo del veredicto

El servicio SHALL determinar `result` con la precedencia: `not_found` > `revoked` > `expired` > `valid`.

Una credencial SHALL considerarse revocada cuando `status = 'revoked'` O `revoked_at IS NOT NULL`.

Una credencial SHALL considerarse expirada cuando (`expires_at IS NOT NULL` AND `expires_at < now`) O `status = 'expired'`. El instante actual MUST obtenerse vía `TimeProvider` en UTC; la expiración MUST computarse desde `expires_at` y no confiar solo en `status`.

#### Scenario: Credencial inexistente

- **WHEN** el `credentialId` no corresponde a ninguna fila de `credentials`
- **THEN** `200 OK` con `result = not_found`
- **AND** `checks.found = false`
- **AND** el bloque `credential` es `null`

#### Scenario: Credencial revocada

- **WHEN** la credencial tiene `status = 'revoked'` o `revoked_at` no nulo
- **THEN** `200 OK` con `result = revoked`
- **AND** `checks.notRevoked = false`

#### Scenario: Credencial expirada por fecha aunque el status no se actualizó

- **WHEN** la credencial tiene `status = 'active'` pero `expires_at` es anterior al instante actual
- **THEN** `200 OK` con `result = expired`
- **AND** `checks.notExpired = false`

#### Scenario: Revocada y expirada simultáneamente

- **WHEN** la credencial está a la vez revocada y expirada
- **THEN** `result = revoked` (la revocación tiene precedencia sobre la expiración)

### Requirement: Registro de cada intento de verificación

Cada llamada a `POST /verifications` SHALL registrar una fila en `verification_logs`, incluido el caso `not_found`.

En `not_found`, la fila MUST tener `credential_id = NULL` y `credential_id_query` igual al UUID consultado. Los sub-booleanos no evaluados (`signature_valid`, `hash_matches`, `on_chain_exists`) MUST guardarse como `NULL`.

#### Scenario: Log en verificación exitosa

- **WHEN** se verifica una credencial existente
- **THEN** se inserta una fila en `verification_logs` con `credential_id` poblado, `credential_id_query` = UUID consultado y `result` correspondiente

#### Scenario: Log en credencial inexistente

- **WHEN** se verifica un UUID que no existe
- **THEN** se inserta una fila en `verification_logs` con `credential_id = NULL`, `credential_id_query` = UUID consultado y `result = not_found`

### Requirement: Errores de entrada como Problem Details

El servicio SHALL responder los veredictos de negocio con `200 OK`; los errores de protocolo/entrada SHALL responder como RFC 7807 Problem Details (`application/problem+json`) con extensión `error`.

Un `credentialId` ausente o con formato de UUID inválido SHALL producir `400` con `error = invalid_credential_id`. El endpoint MUST NOT usar `401` ni `404` para veredictos de negocio.

#### Scenario: UUID malformado

- **WHEN** se hace `POST /verifications` con `credentialId` que no es un UUID válido
- **THEN** `400` Problem Details con `error = invalid_credential_id`

#### Scenario: credentialId ausente

- **WHEN** el cuerpo no incluye `credentialId`
- **THEN** `400` Problem Details con `error = invalid_credential_id`
