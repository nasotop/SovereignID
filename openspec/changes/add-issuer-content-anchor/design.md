## Context

El flujo de emisión actual (`TitleIssuanceService` en Angular):

1. `VcDocumentService.buildTitleCredential()` construye el VC JSON-LD.
2. `IpfsPinningService.pinJson()` canonicaliza con `JSON.stringify` plano, hashea, y pinnea vía Pinata **o** genera CID fake.
3. `CredentialContractService` ancla on-chain (MetaMask).
4. `IssuerApiService.linkStudentTitle()` persiste en BD; issuer valida strings de IPFS pero **no** existencia de contenido.

El verifier (`add-verifier-evidence-checks`) ya implementa `HttpIpfsContentReader`: descarga `ipfs_gateway_url`, hashea bytes crudos y compara con `content_hash`. Con CID fake o re-serialización Pinata, `hashMatches` falla.

Decisiones cerradas en sesión de grilling (2026-07-09):

| Tema | Decisión |
|------|----------|
| Seam | issuer-api (`CredentialContentAnchor`) |
| Sin Pinata | Fail hard; Pinata free tier documentado |
| Adaptadores | `IContentPinningAdapter` (Pinata + InMemory) |
| Verify en link | `IContentAnchorVerifier` con flag |
| Ruta | `POST /issuer/institutions/{id}/documents/anchor` + JWT `InstitutionIssuer` |
| Canonicalización | JCS RFC 8785 (`Corvus.Text.Json`) |
| Compatibilidad | Reset `seed-dev.sql`; solo forward |
| Gateway | `GatewayBase` configurable por entorno |
| Pinata auth | API Key + Secret |
| Pin API | `pinFileToIPFS` con bytes JCS UTF-8 |
| Flags | Base `false`/`false`; dev/docker `true`/`true` |
| VC builder | Sigue en frontend (`VcDocumentService`) |

Patrón de referencia en el monorepo: `IBlockchainAnchorVerifier` / `RpcBlockchainAnchorVerifier` / `NullBlockchainAnchorVerifier` en issuer.

## Goals / Non-Goals

**Goals:**

- Concentrar canonicalización JCS, hash SHA-256 y pinning detrás de una interfaz pequeña (`CredentialContentAnchor`).
- Eliminar CID fake y JWT Pinata en el navegador (limpieza en cambio frontend posterior).
- Permitir que issuer rechace anclajes de contenido inválidos antes de INSERT en `credentials`.
- Alinear bytes pinneados con lo que `HttpIpfsContentReader` del verifier espera.
- Tests de integración issuer sin red externa (`InMemoryContentPinningAdapter`).

**Non-Goals:**

- Construir el VC JSON-LD en backend (el frontend sigue enviando el documento).
- Nodo IPFS propio o multi-gateway en verifier.
- Adapter dev determinístico con CID fake (fail hard).
- Versionado `contentHashAlgorithm` para credenciales legacy.
- Cambios en portal Angular (cambio `add-frontend-content-anchor`).

## Decisions

### D1: Módulo profundo `CredentialContentAnchor` en issuer.Application

**Decisión:** Una interfaz de aplicación con un método:

```csharp
Task<ContentAnchorResult> AnchorAsync(JsonElement document, CancellationToken ct);
```

La implementación interna orquesta: JCS (`Corvus.Text.Json.JsonCanonicalizer`) → SHA-256 hex (`0x` + 64 chars) → `IContentPinningAdapter.PinAsync(bytes)` → construir `gatewayUrl` desde `GatewayBase`.

**Alternativa rechazada:** mantener pinning en Angular con fail-hard. El API key seguiría expuesto si se usara Pinata desde browser; no concentra canonicalización en un solo lugar.

### D2: Seam interno `IContentPinningAdapter`

| Adapter | Rol |
|---------|-----|
| `PinataContentPinningAdapter` | `multipart/form-data` a `pinFileToIPFS` con bytes JCS; auth `pinata_api_key` + `pinata_secret_api_key` headers |
| `InMemoryContentPinningAdapter` | Devuelve CID determinístico + almacena bytes en memoria para tests de verify |
| Wiring sin credenciales | No registrar Pinata; `AnchorAsync` → `503 ipfs_not_configured` |

**Regla:** un adapter de producción + uno in-memory justifica el seam (deletion test: tests no llaman a Pinata).

### D3: JCS (RFC 8785) con `Corvus.Text.Json`

**Decisión:** `JsonCanonicalizer.Canonicalize(JsonElement)` produce bytes UTF-8; esos mismos bytes se hashean y se suben a Pinata.

**Alternativa rechazada:** `pinJSONToIPFS` — Pinata re-serializa y rompe bytes-exactos.

**Alternativa rechazada:** `JSON.stringify` legacy — inconsistente con estándar W3C y con verifier.

### D4: `IContentAnchorVerifier` en `LinkStudentTitleAsync`

**Decisión:** Tras validar payload y antes de INSERT, si `ContentAnchor:VerifyEnabled=true`, GET `ipfsGatewayUrl` + comparar SHA-256 con `contentHash`. Si falla → `409 content_anchor_invalid`.

Patrón idéntico a `IBlockchainAnchorVerifier` con `NullContentAnchorVerifier` cuando `VerifyEnabled=false`.

**Motivo:** el endpoint `/title` no recibe DID ni contenido; un cliente podría enviar CID/hash de terceros. El anchor endpoint cubre el flujo feliz del portal, pero el contrato HTTP de link debe poder rechazar inconsistencias.

### D5: Ruta institucional scoped + JWT

`POST /issuer/institutions/{institutionId}/documents/anchor`

- Política `InstitutionIssuer`.
- Resolver membership `{institutionId}:admin|issuer` (o `platform_admin`) del JWT.
- Body: `{ "document": <JsonElement VC JSON-LD> }`.
- Response `200`: `{ contentHash, ipfsCid, ipfsGatewayUrl }`.

Documentar Bearer en OpenAPI para esta operación (alineación futura con holder endpoints).

### D6: Configuración `Issuer:ContentAnchor`

```json
{
  "Issuer": {
    "ContentAnchor": {
      "Enabled": false,
      "VerifyEnabled": false,
      "GatewayBase": "https://ipfs.io/ipfs",
      "PinataApiKey": "",
      "PinataApiSecret": "",
      "IpfsTimeoutSeconds": 30
    }
  }
}
```

`appsettings.Development.json` / docker-compose: `Enabled=true`, `VerifyEnabled=true`, credenciales Pinata vía env.

### D7: Catálogo de errores nuevos

| `error` | HTTP | Cuándo |
|---------|------|--------|
| `ipfs_not_configured` | 503 | `Enabled=false` o sin credenciales Pinata |
| `content_anchor_failed` | 502 | Pinata/red falló al pin |
| `invalid_anchor_document` | 400 | JSON vacío o no objeto/array top-level JCS |
| `content_anchor_invalid` | 409 | Verify en link: gateway no coincide con hash |

### D8: Sin paquete compartido issuer↔verifier

La regla de hash (JCS bytes → SHA-256) se documenta en `docs/issuer-domain-contract.md`. Verifier no recanonicaliza; confía en bytes del gateway. Tests de issuer incluyen vectores RFC 8785 para el serializador.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| Pinata caído bloquea emisión | Fail hard explícito antes de MetaMask; mensaje `content_anchor_failed` |
| Gateway público lento en verify | `GatewayBase` Pinata dedicado en staging/prod |
| Credenciales seed desalineadas | Actualizar `seed-dev.sql` en este cambio |
| Frontend aún usa `IpfsPinningService` hasta cambio 2 | Documentar orden de despliegue: backend primero |
| JCS más estricto que JSON ad-hoc | VC debe ser I-JSON subset; validar en tests con documentos reales del frontend |

## Migration Plan

1. Desplegar issuer-api + BFF con endpoint anchor (flags `false` en prod inicialmente).
2. Configurar Pinata en staging; activar `Enabled=true`.
3. Merge `add-frontend-content-anchor` para consumir el endpoint.
4. Activar `VerifyEnabled=true` en staging; validar con verifier `IpfsCheckEnabled=true`.
5. Reset/re-emitir credenciales de demo con nuevo algoritmo.

**Rollback:** desactivar `Enabled`/`VerifyEnabled`; frontend legacy sigue funcionando hasta que se mergee el cambio 2 (ventana corta).

## Open Questions

- Ninguna pendiente — decisiones cerradas en grilling.
