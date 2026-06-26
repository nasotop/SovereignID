## Why

El microservicio `verifier` solo expone `GET /health`; no aporta valor de negocio todavía. La capacidad más importante de la plataforma es permitir que **cualquier tercero (verificador, típicamente anónimo) compruebe la validez de una Verifiable Credential** emitida a un estudiante. Sin este endpoint, una credencial "soberana y verificable" no puede verificarse de forma independiente.

Aprovechamos además que la base de datos canónica ya está disponible para conectar el verifier a datos reales, y para retirar el almacén `InMemory` (que nació como artefacto de demo) en favor de una persistencia única respaldada por EF.

## What Changes

- Nuevo endpoint **`POST /verifications`** (público/anónimo) que recibe `{ "credentialId": "<uuid>" }` y devuelve un **veredicto de verificación** (`200 OK` con campo `result`).
- Veredicto v1 escalonado: chequeos **sin dependencia externa** (`found`, `notRevoked`, `notExpired`) computados contra la BD; los chequeos con red (`hashMatches`, `onChainExists`, `signatureValid`) se devuelven como `null` (reservados para iteraciones futuras).
- `result` ∈ { `valid`, `revoked`, `expired`, `not_found` } en v1; `invalid_signature`, `tampered`, `ipfs_unreachable` quedan reservados en el contrato.
- Precedencia de veredicto: `not_found` > `revoked` > `expired` > `valid`. La expiración se **computa desde `expires_at`** (no se confía solo en `status`).
- Cada intento se registra en `verification_logs`, incluido `not_found` (`credential_id = NULL`, `credential_id_query` = UUID consultado).
- Errores de negocio = `200` con `result`; Problem Details (RFC 7807) **solo** para entrada malformada → `400` con `error = invalid_credential_id`.
- Persistencia del verifier **Postgres-only**, con **modelo EF propio** (scaffold acotado a `credentials`, `verification_logs`, `credential_types`, `institutions`), `DbContext` independiente del de `auth`.
- Stores del verifier vía interfaces en Application (`ICredentialReadStore`, `IVerificationLogStore`) con un único adapter EF (`EfCredentialReadStore`, `EfVerificationLogStore`).
- Tests de integración con **Testcontainers PostgreSQL** (BD efímera + esquema canónico + datos sembrados).
- **BREAKING (interno):** se retira el proveedor `InMemory` de `auth`. Se elimina `InMemoryChallengeStore`, el toggle `Persistence:Provider` y la rama de selección; `PostgresChallengeStore` se renombra a `EfChallengeStore`. Los tests AC-01…AC-07 pasan a correr contra Postgres (Testcontainers).

## Capabilities

### New Capabilities

- `verifier-credential-verification`: Contrato HTTP y reglas de dominio del endpoint `POST /verifications` (entrada por UUID, cómputo del veredicto, precedencia, catálogo de errores, registro en `verification_logs`).
- `verifier-infrastructure-persistence`: Convenciones de persistencia del verifier — modelo EF propio y acotado (database-first), `DbContext` independiente, stores Postgres-only vía interfaces de Application.

### Modified Capabilities

- `auth-infrastructure-persistence-layout`: Se retira el proveedor `InMemory` y el toggle `Persistence:Provider`; queda una persistencia única respaldada por EF, con `IChallengeStore` implementado por `EfChallengeStore`.

## Impact

- **Código nuevo (`src/verifier/`):** `Verifier.Api` (controller `VerificationsController`, modelos, filtro Problem Details), `Verifier.Application` (caso de uso de verificación, interfaces de store, opciones), `Verifier.Domain` (veredicto, códigos de error), `Verifier.Infrastructure` (modelo EF generado, adapters EF, composición DI).
- **Código modificado (`src/auth/`):** eliminación de `InMemoryChallengeStore`, renombrado a `EfChallengeStore`, simplificación de `AddAuthPersistence`/`DependencyInjection` (sin selector de proveedor).
- **Tooling/persistencia:** nuevo `efcpt-config.json` + script de scaffold para verifier; modelo acotado a 4 tablas.
- **Contratos:** nuevo snapshot `docs/contracts/verifier.openapi.json` (con `POST /verifications`); nuevo contrato de dominio `docs/verifier-backend-contract.md`.
- **Documentación/decisiones:** `CONTEXT.md` (sección verifier, glosario, tabla de config sin `Persistence:Provider`); **ADR-0003** que supersede a ADR-0002 en el punto del proveedor.
- **Tests:** nuevo `tests/verifier/Verifier.IntegrationTests` (Testcontainers); migración de `Auth.IntegrationTests` a Postgres/Testcontainers.
- **Dependencias:** `Testcontainers.PostgreSql` en los proyectos de test; el verifier referencia Npgsql/EF como `auth`.
