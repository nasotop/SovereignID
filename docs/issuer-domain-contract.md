# Issuer service domain contract

El servicio `issuer` concentra la emision, consulta y gobernanza de credenciales verificables del MVP.

## Alcance MVP

- Vincular un titulo emitido a un estudiante mediante la tabla `credentials`.
- Asociar el titulo emitido a una carrera activa de la misma institucion cuando el tipo de credencial lo requiera.
- Vincular la wallet/DID emisor de una institucion existente.
- Usar la wallet primaria activa del estudiante como sujeto de la credencial.
- Usar el DID emisor de la institucion asociada al estudiante.
- Consultar credenciales del titular autenticado (portal Holder) filtrando por `subject_did` del JWT SIWE.

## Endpoints

| Metodo | Ruta | Proposito |
|--------|------|-----------|
| `POST` | `/issuer/institutions/{institutionId}/wallet` | Vincula la wallet/DID emisor de la institucion |
| `POST` | `/issuer/institutions/{institutionId}/documents/anchor` | Canonicaliza (JCS RFC 8785), calcula `contentHash` y pinnea el VC en IPFS |
| `POST` | `/issuer/students/{studentId}/title` | Registra un titulo emitido y lo vincula al estudiante |
| `GET` | `/issuer/holders/me/credentials` | Lista credenciales del titular autenticado (JWT) |
| `GET` | `/issuer/holders/me/credentials/{credentialId}` | Detalle de una credencial del titular autenticado |
| `GET` | `/issuer/credentials/{credentialId}` | Detalle autenticado si la credencial pertenece al titular del JWT |

## Autenticacion y autorizacion

- Los endpoints holder requieren JWT SIWE con politica `HolderAuthenticated` (`holder=true` o claim `did`).
- El filtro de titularidad usa el claim `did` del JWT contra `credentials.subject_did`.
- Las credenciales emitidas a wallets rotadas siguen siendo visibles porque el filtro no depende de la wallet primaria actual.
- Los endpoints institucionales (`POST /issuer/institutions/{id}/wallet`, `POST /issuer/institutions/{id}/documents/anchor`, `POST /issuer/students/{id}/title`, `GET /issuer/institutions/{id}/credentials`, `POST /issuer/credentials/{id}/revoke`) requieren politica `InstitutionIssuer`: membership `{institutionId}` con rol `admin` o `issuer` en el JWT (o `platform_admin`).
- Revoke sin JWT devuelve `401`; JWT sin membership devuelve `403`.
- Issuer valida JWT localmente con la misma clave/`iss`/`aud` que Auth (`Auth:JwtSigningKey`, `Auth:JwtIssuer`, `Auth:JwtAudience`).

## Anclaje de contenido (IPFS)

El endpoint `POST .../documents/anchor` acepta `{ "document": <VC JSON-LD> }` construido por el emisor frontend.

1. Canonicaliza el documento con **JCS (RFC 8785)** vía `Corvus.Text.Json`.
2. Calcula `contentHash` = `0x` + SHA-256 hex (minúsculas) de los **bytes UTF-8 canónicos**.
3. Pinnea esos mismos bytes con Pinata `pinFileToIPFS` (API Key + Secret server-side).
4. Responde `{ contentHash, ipfsCid, ipfsGatewayUrl }` donde `ipfsGatewayUrl = {GatewayBase}/{ipfsCid}`.

Configuración `Issuer:ContentAnchor`: `Enabled`, `VerifyEnabled`, `GatewayBase`, `PinataApiKey`, `PinataApiSecret`, `IpfsTimeoutSeconds`. Defaults base `Enabled=false` / `VerifyEnabled=false`; Development/docker-compose activan ambos.

Cuando `VerifyEnabled=true`, `POST /students/{id}/title` descarga el gateway y exige que el SHA-256 de los bytes coincida con `contentHash` antes de INSERT (`409 content_anchor_invalid` si falla).

## Contrato OpenAPI (HTTP)

Snapshot versionado: `docs/contracts/issuer.openapi.json` (regenerar con `bash scripts/export-openapi.sh issuer`; CI valida con `verify-openapi`).

| Elemento | Valor |
|----------|-------|
| Esquema de seguridad | `components.securitySchemes.bearerAuth` — HTTP Bearer, formato JWT |
| Operaciones protegidas en el contrato | Holder reads + `POST /issuer/institutions/{institutionId}/documents/anchor` |
| Campo `status` (holder) | `enum`: `active`, `revoked`, `expired` en `HolderCredentialSummary` y `HolderCredentialDetail` |

El portal web holder consume estos endpoints via cliente Angular generado (`ng-openapi-gen`) y fachada `HolderService`; nginx proxea `/issuer/` hacia `issuer-api` (o `/api/issuer/` vía BFF).

Pass-through BFF: `POST /api/issuer/institutions/{institutionId}/documents/anchor` → issuer-api (reenvía `Authorization`).

## Reglas principales

1. El backend no crea cuentas MetaMask ni wallets.
2. La institucion puede vincular una wallet MetaMask existente como wallet emisora.
3. El estudiante debe existir, estar activo y tener wallet primaria activa.
4. La institucion del estudiante debe existir, estar activa y tener DID emisor.
5. Para emision de titulo, la carrera se selecciona desde el pool de `academy`; si se informa una carrera, debe pertenecer a la misma institucion y estar activa.
6. El tipo de credencial debe existir y estar activo.
7. El request de titulo debe incluir `careerId`, CID IPFS, gateway URL, hash de contenido, transaction hash, block number y firma EIP-712.
8. Si `chainId` no viene en el request, se usa `Issuer:DefaultChainId`.
9. Las consultas de solo lectura en Infrastructure usan LINQ con `AsNoTracking`.
10. La API no inyecta `DbContext`; Application usa `ITitleIssuerRepository`, `ICredentialReadStore` y los adapters EF viven en Infrastructure.
11. Issuer valida JWT localmente con la misma clave/`iss`/`aud` que Auth (`Auth:JwtSigningKey`, `Auth:JwtIssuer`, `Auth:JwtAudience`).

## Errores de dominio

| Error | HTTP | Causa |
|-------|------|-------|
| `invalid_institution` | 400 | `institutionId` vacio |
| `invalid_issuer_wallet` | 400 | Wallet/DID emisor incompleto |
| `issuer_wallet_link_failed` | 409 | La institucion no existe o no esta activa |
| `invalid_student` | 400 | `studentId` vacio |
| `invalid_credential_type` | 400 | Tipo de credencial vacio |
| `invalid_title_payload` | 400 | Payload incompleto para emitir/vincular titulo |
| `title_link_failed` | 409 | No existe estudiante, wallet, DID emisor, carrera o tipo de credencial valido |
| `content_anchor_invalid` | 409 | Verify IPFS en link: gateway inalcanzable o hash distinto |
| `ipfs_not_configured` | 503 | `ContentAnchor:Enabled=false` o faltan credenciales Pinata |
| `content_anchor_failed` | 502 | Pinata/red falló al pinnear |
| `invalid_anchor_document` | 400 | `document` vacío o no objeto/array top-level válido para JCS |
| `unauthenticated` | 401 | Falta JWT o token invalido |
| `forbidden` | 403 | JWT valido pero sin membership/rol requerido |
| `credential_not_found` | 404 | Credencial inexistente o no pertenece al titular autenticado |

## Persistencia

- Modelo EF database-first con efcpt en `Issuer.Infrastructure/Persistence/Generated/`.
- Regenerar: `scripts/scaffold-issuer-db.ps1` (requiere Postgres healthy).
