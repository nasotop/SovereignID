## Why

La emisión de credenciales hoy ancla el documento verificable desde el navegador (`IpfsPinningService`): sin JWT de Pinata en `localStorage` genera un **CID fake** (`bafy…dev`) que issuer persiste como si fuera real. El verifier (`HttpIpfsContentReader`) entonces devuelve `hashMatches=false` porque el gateway no resuelve contenido. Además, el hash se calcula con `JSON.stringify` plano mientras Pinata puede re-serializar vía `pinJSONToIPFS`, rompiendo la coherencia bytes-exactos que el verifier exige. Hay que mover el anclaje IPFS al **issuer-api**, eliminar el fallback silencioso y alinear canonicalización (JCS) con verificación.

## What Changes

- Nuevo módulo profundo **`CredentialContentAnchor`** en `issuer.Application`: canonicaliza (JCS RFC 8785), calcula `contentHash`, pinnea vía adaptador y devuelve `{ contentHash, ipfsCid, gatewayUrl }`.
- Nuevo endpoint **`POST /issuer/institutions/{institutionId}/documents/anchor`** con política `InstitutionIssuer` (JWT + membership de la institución).
- Adaptadores **`IContentPinningAdapter`**: `PinataContentPinningAdapter` (`pinFileToIPFS` con bytes JCS), `InMemoryContentPinningAdapter` (tests), fail-hard si no hay credenciales Pinata (`503 ipfs_not_configured`).
- Nuevo **`IContentAnchorVerifier`** simétrico a `IBlockchainAnchorVerifier`: verifica en `POST /title` que el gateway devuelve bytes cuyo SHA-256 coincide con `contentHash` (flags `VerifyEnabled`).
- Dependencia **`Corvus.Text.Json`** para JCS; auth Pinata server-side con **API Key + Secret**.
- Config **`Issuer:ContentAnchor`**: `Enabled`, `VerifyEnabled`, `GatewayBase`, `PinataApiKey`, `PinataApiSecret`; defaults `false`/`false` en template base, `true`/`true` en `Development`/`docker-compose`.
- BFF pass-through del nuevo endpoint; snapshot **`docs/contracts/issuer.openapi.json`** y cliente Kiota regenerados.
- Actualizar **`database/seed-dev.sql`** con hashes coherentes JCS (sin versionado legacy de algoritmo).
- **BREAKING (semántica de emisión):** credenciales emitidas con el algoritmo anterior (`JSON.stringify` / CID fake) no pasarán `hashMatches` cuando `IpfsCheckEnabled=true`; se asume reset de datos de demo.

## Capabilities

### New Capabilities

- `issuer-content-anchor`: endpoint de anclaje institucional, canonicalización JCS, pinning Pinata/in-memory, configuración y errores de dominio.
- `issuer-title-content-verification`: verificación opcional de contenido IPFS al vincular título (`POST /students/{id}/title`), integrada en `IssuerService`.

### Modified Capabilities

<!-- Sin deltas en openspec/specs/ archivados: la emisión institucional aún no tiene spec archivada; este cambio introduce las capabilities nuevas. -->

## Impact

- **Backend (`issuer`):** `Issuer.Application`, `Issuer.Infrastructure` (nuevo namespace `ContentAnchor/`), `Issuer.Api` (controller + models), `Issuer.IntegrationTests`.
- **BFF:** `InstitutionDocumentsController` (o equivalente), Kiota client, `bff.openapi.json`.
- **Dependencias:** NuGet `Corvus.Text.Json`.
- **Config / ops:** `.env.example`, `appsettings.Development.json`, `docker-compose` env vars para Pinata.
- **Docs:** `docs/issuer-domain-contract.md`, `docs/deployment.md`, `CONTEXT.md` (sección issuer).
- **Fuera de alcance de este cambio:** portal Angular (cambio `add-frontend-content-anchor`), nodo IPFS self-hosted, adapter dev con CID fake, versionado `contentHashAlgorithm` legacy.
