## Context

El monorepo SovereignID ya tiene operativo el microservicio `auth` (SIWE → JWT) con una arquitectura por capas (`Api`/`Application`/`Domain`/`Infrastructure`), errores RFC 7807, snapshots OpenAPI versionados y persistencia database-first sobre la BD canónica `database/BBDD_SovereignID.sql`. El microservicio `verifier` está scaffolded pero vacío (solo `GET /health`).

La BD canónica ya está disponible y modela el dominio de verificación: `credentials` (ancla de cada VC, con `status`, `expires_at`, `revoked_at`, anchors on-chain), `verification_logs` (auditoría de cada intento, con enum `verification_result`), `credential_types` e `institutions`.

Esta entrega añade el primer endpoint de negocio del verifier (`POST /verifications`) y, de paso, retira el proveedor de persistencia `InMemory` (artefacto de demo) del monorepo.

## Goals / Non-Goals

**Goals:**
- Endpoint público `POST /verifications` que verifica una VC por su UUID y devuelve un veredicto.
- Veredicto v1 computado contra la BD: `found`, `notRevoked`, `notExpired`.
- Modelo EF propio del verifier (acotado a 4 tablas), independiente del de `auth`.
- Persistencia Postgres-only; tests con Testcontainers.
- Retirar `InMemory` de `auth` y renombrar el adapter a `EfChallengeStore`.

**Non-Goals:**
- Chequeos con dependencia externa: fetch IPFS (`hashMatches`), consulta on-chain Sepolia (`onChainExists`), verificación de firma EIP-712 (`signatureValid`/`tampered`). Se devuelven `null` y quedan reservados.
- Envío de la VC completa (JSON-LD) para verificación offline.
- Rate limiting / anti-abuso del endpoint anónimo (se anota como riesgo).
- Resolución DID on-chain.
- Endpoints de emisión o revocación de credenciales.

## Decisions

### D1: Entrada por UUID (`credentials.id`), no por CID ni por VC completa
El identificador público canónico de la VC es `credentials.id` ("UUID que también es el credentialId del VC"). Es lo que va en el QR. El `ipfs_cid` es destino de fetch interno, no entrada del verificador.
**Alternativas:** (a) IPFS CID — es detalle de almacenamiento; (b) VC completa JSON-LD — añade parsing y no encaja con `verification_logs.credential_id_query`.

### D2: `POST /verifications` (no `GET /credentials/{id}/verification`)
La verificación **crea** un recurso auditado (`verification_logs`), no es lectura pura. POST es honesto semánticamente, extensible (futuros chequeos/params en el body) y consistente con `POST /auth/verify`.
**Alternativa:** `GET /{id}` favorecería QR navegable, pero el QR lo consume el frontend que hace la llamada HTTP, así que no se necesita una URL navegable.

### D3: Veredicto negativo = `200 OK` con `result`; Problem Details solo para entrada malformada
Una credencial revocada/expirada/inexistente es el **resultado legítimo** de una operación ejecutada con éxito, no un fallo de protocolo. El enum `verification_result` (incluye `not_found`) y el registro de todo intento en `verification_logs` confirman este modelo.
**Matiz vs CONTEXT.md:** la sección transversal "Modelo de errores HTTP" asume fallos de negocio = Problem Details; en `verifier` Problem Details cubre **solo** errores de entrada/protocolo (`400`). Esto se documenta explícitamente.

### D4: Veredicto escalonado v1 (solo chequeos sin red)
`found`, `notRevoked`, `notExpired` se computan contra la BD. `hashMatches`, `onChainExists`, `signatureValid` → `null` ("no evaluado"). Permite entregar valor sin acoplar a IPFS/nodo Sepolia/cripto.

### D5: Precedencia y cómputo del veredicto
Precedencia: `not_found` > `revoked` > `expired` > `valid`.
- `revoked` = `status = 'revoked'` OR `revoked_at IS NOT NULL`.
- `expired` = (`expires_at IS NOT NULL` AND `expires_at < now`) OR `status = 'expired'`.
- `valid` = existe, no revocada, no expirada.
La expiración se **computa desde `expires_at`** vía `TimeProvider` (no se confía solo en el enum, evitando el bug del job nocturno que no corrió). Revocada gana sobre expirada (acto intencional, señal más fuerte).

### D6: Persistencia Postgres-only con modelo EF propio y acotado
Scaffold database-first (EF Core Power Tools) en `Verifier.Infrastructure/Persistence/Generated/`, `DbContext` propio (`VerifierDbContext`, `internal`), namespace `Verifier.Infrastructure.Persistence.Generated.Entities`, **acotado a 4 tablas**: `credentials`, `verification_logs`, `credential_types`, `institutions`. No se comparte el modelo generado entre microservicios.
Stores vía interfaces en Application: `ICredentialReadStore` (lee credencial + joins tipo/emisor) y `IVerificationLogStore` (escribe el intento). Un único adapter EF por interfaz: `EfCredentialReadStore`, `EfVerificationLogStore`.

### D7: Sin proveedor InMemory en el monorepo
Se elimina `InMemoryChallengeStore` y el toggle `Persistence:Provider`; `PostgresChallengeStore` → `EfChallengeStore`. InMemory era grado demo (no durable, no multi-instancia). Esto revierte parte de ADR-0002 → se registra en **ADR-0003** (supersede el punto del proveedor) y se actualiza `auth-infrastructure-persistence-layout`.

### D8: Tests con Testcontainers PostgreSQL
Los tests de integración (verifier y auth) levantan un Postgres efímero, aplican `database/BBDD_SovereignID.sql`, siembran datos y ejercen los endpoints de punta a punta. Evita reintroducir mocks InMemory y no acopla CI a un compose.

### D9: Endpoint anónimo
La verificación es pública por diseño de dominio (`verification_logs.verifier_ip` = "verificador anónimo"). No requiere JWT.

## Risks / Trade-offs

- **Endpoint anónimo abusable (scraping/enumeración de UUID)** → Mitigación: rate limiting / throttling como trabajo futuro; los UUID v4 no son enumerables trivialmente; `verification_logs` permite detectar abuso.
- **Migrar AC-01…AC-07 a Postgres puede hacer los tests más lentos/frágiles** → Mitigación: una sola instancia de contenedor compartida por colección de tests; aplicar el esquema una vez.
- **Discrepancia `status` vs fechas en datos reales** → Mitigada por D5 (cómputo desde `expires_at`); se documenta el invariante.
- **`result` de un solo valor pierde información cuando hay múltiples condiciones** → Aceptado; los booleanos `checks` exponen el detalle completo además del `result` resumido.
- **Reversión de ADR-0002** puede confundir a quien lo lea aislado → Mitigación: ADR-0003 lo supersede explícitamente y enlaza.

## Migration Plan

1. Crear ADR-0003 (supersede ADR-0002 en el proveedor) y actualizar `CONTEXT.md`.
2. Refactor `auth`: eliminar InMemory + toggle, renombrar a `EfChallengeStore`, simplificar composición DI.
3. Migrar `Auth.IntegrationTests` a Testcontainers; verificar AC-01…AC-07 verdes.
4. Scaffold del modelo EF acotado del verifier + `VerifierDbContext`.
5. Implementar dominio/aplicación/infra del verifier y `POST /verifications`.
6. Exportar snapshot `docs/contracts/verifier.openapi.json` y escribir `docs/verifier-backend-contract.md`.
7. Tests de integración del verifier (Testcontainers).

Rollback: el verifier es aditivo (revertible eliminando el endpoint); el refactor de `auth` se revierte restaurando el adapter InMemory y el toggle si fuese necesario.

## Open Questions

- Nombre definitivo del `DbContext` del verifier: propuesto `VerifierDbContext`.
- ¿Sembrar datos de prueba vía SQL fixture o builders C# en los tests? (a decidir en implementación; no bloquea el contrato).
