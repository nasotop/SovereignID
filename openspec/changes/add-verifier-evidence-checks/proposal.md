## Why

El servicio `verifier` promete verificación pública de credenciales académicas "en la blockchain", pero hoy `VerifyCredentialUseCase` solo cruza contra la base de datos propia: los tres chequeos con dependencia externa (`hashMatches`, `onChainExists`, `signatureValid`) están reservados como `null` desde v1. Un ente validador externo (empresa, otra institución) no tiene hoy ninguna garantía criptográfica real de que el título no fue falsificado o alterado — solo confía en el índice interno de SovereignID. Hay que cerrar esa brecha implementando los tres chequeos contra sus fuentes de verdad reales (contrato `CredentialRegistry` en Sepolia, gateway IPFS, firma EIP-712), sin introducir un vector de confianza falso cuando esas fuentes fallan o son parcialmente inconsistentes.

## What Changes

- **BREAKING (contrato):** el enum `result` de `POST /verifications` gana un quinto valor, **`integrity_failed`**, para veredictos donde al menos un chequeo evaluado (no `null`) dio `false`. Precedencia completa: `not_found > revoked > expired > integrity_failed > valid`.
- Implementar `onChainExists`: `eth_call` a `getCredential` del contrato `CredentialRegistry`; `true` solo si el registro existe **y** es coherente con BD (`contentHash`, `ipfsCid`, `institutionId`, `issuer`, `subject`). Un registro incoherente cuenta como `false`, no como `true` parcial.
- Implementar `hashMatches`: descarga el documento desde `ipfs_gateway_url` (un solo gateway, sin fallback), calcula SHA-256 sobre los bytes exactos recibidos y compara contra `content_hash`. Timeout ⇒ `null`.
- Implementar `signatureValid` con jerarquía de fuente **asimétrica**: on-chain (`issuer` del contrato) es la fuente primaria y la única que puede **confirmar** (`true`); el fallback a BD (`institutions.issuer_wallet_address`) solo puede **rechazar** (`false`) o quedar **inconcluso** (`null`) — nunca confirma por sí solo. Si el registro on-chain existe pero es incoherente, no se usa su `issuer` (se trata como on-chain no disponible para este propósito).
- Nuevo campo `validationSource` en `checks` (`on_chain | bd_fallback_inconclusive | bd_fallback_rejected | not_evaluated`), trazando de qué fuente vino el veredicto de `signatureValid`.
- Nuevo campo `revocationSource` en `checks` (`bd | on_chain | both`): on-chain puede **elevar** una credencial a `revoked` aunque BD diga `active`; BD revocada nunca se "des-revoca" por on-chain.
- Tres flags de configuración independientes (`OnChainCheckEnabled`, `IpfsCheckEnabled`, `SignatureCheckEnabled`), todos `false` por defecto — activación incremental por fases sin cambiar código.
- **Rate limiting** en `Verifier.Api` (token bucket por IP: capacidad 10, recarga 1/3s) dado que el endpoint público ahora dispara llamadas RPC/IPFS costosas por request. Requiere `ForwardedHeadersMiddleware` con `KnownProxies`/`KnownNetworks` restringidos a la red interna (nginx + bff-api) para identificar la IP real del cliente.
- `verification_logs` gana columnas para persistir `signature_validation_source` y `revocation_source`, además de rellenar `signature_valid`, `hash_matches`, `on_chain_exists` (hoy siempre `NULL`).

## Capabilities

### New Capabilities

- `verifier-evidence-verification`: lógica de verificación criptográfica de evidencia (on-chain, IPFS, firma EIP-712) que alimenta los chequeos `onChainExists`, `hashMatches`, `signatureValid`, con sus reglas de fuente, fallback asimétrico y trazabilidad (`validationSource`, `revocationSource`), y el nuevo veredicto `integrity_failed`.
- `verifier-request-rate-limiting`: límite de tasa por IP (token bucket) sobre el endpoint público `POST /verifications` de `Verifier.Api`, incluyendo resolución de IP real detrás de proxy.

### Modified Capabilities

- `verifier-credential-verification`: el enum `result` pasa de 4 a 5 valores (agrega `integrity_failed`) y la precedencia de veredicto se extiende para incluirlo. *(Nota: esta capability está definida en el cambio pendiente `add-verifier-portal-client`, aún no archivado en `openspec/specs/`; este cambio asume que se archivará antes o junto con este, y su spec delta aquí se aplica sobre esa base.)*

## Impact

- **Backend (`Verifier`):** `Verifier.Application/VerifyCredentialUseCase.cs` (orquestación extendida, sin exponer RPC/IPFS/crypto en su interfaz), nuevo `ICredentialEvidenceVerifier` + adapters (`RpcCredentialRegistryReader`, `HttpIpfsContentReader`, `NethereumEip712IssuanceSignatureVerifier`) en `Verifier.Infrastructure`, extensión de `CredentialReadModel`/`EfCredentialReadStore` (joins a `institutions.issuer_wallet_address`, `student_wallets.wallet_address`, `credentials.eip712_signature`), `VerifierOptions` (tres flags + config RPC/IPFS), rate limiting middleware en `Verifier.Api`.
- **Contrato:** `docs/contracts/verifier.openapi.json` — nuevo valor de enum `result`, nuevos campos `validationSource`/`revocationSource` en `VerificationChecks`, nueva respuesta `429`. Reexportar snapshot.
- **Base de datos:** migración en `database/` — nuevas columnas en `verification_logs` (`signature_validation_source`, `revocation_source`).
- **Frontend:** **fuera de alcance** de este cambio (backend-only); el cliente Angular generado se regenera mecánicamente pero la UX de los nuevos campos/veredicto queda para un cambio posterior.
- **Fuera de alcance:** alerta de drift institucional (`issuer_wallet_address` BD vs on-chain) — trabajo futuro, naturaleza proactiva/institucional distinta a este cambio reactivo/por-credencial. Modo estricto "exigir on-chain" por tipo de credencial — sin caso de uso confirmado hoy. Paquete común compartido con `Issuer` — solo un consumidor real (`Verifier`) por ahora.
