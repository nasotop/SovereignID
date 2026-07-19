## ADDED Requirements

### Requirement: Anclaje institucional de documento verificable

El servicio `issuer` SHALL exponer:

| Método | Ruta | Propósito |
|--------|------|-----------|
| `POST` | `/issuer/institutions/{institutionId}/documents/anchor` | Canonicaliza (JCS), calcula `contentHash` y pinnea el VC JSON-LD en IPFS |

La operación SHALL requerir JWT con política `InstitutionIssuer` y membership activa para `{institutionId}` (rol `admin` o `issuer`, o `platform_admin`).

El body SHALL aceptar un objeto JSON con campo `document` (VC JSON-LD construido por el emisor frontend).

La respuesta `200` SHALL incluir:

- `contentHash`: SHA-256 de los bytes UTF-8 canónicos JCS, formato `0x` + 64 hex minúsculas.
- `ipfsCid`: CID devuelto por el adaptador de pinning.
- `ipfsGatewayUrl`: `{GatewayBase}/{ipfsCid}` donde `GatewayBase` es configuración del servicio.

#### Scenario: Anclaje exitoso con Pinata configurado

- **WHEN** `ContentAnchor:Enabled=true`, credenciales Pinata válidas, y un emisor institucional autenticado envía un `document` VC JSON-LD válido
- **THEN** el servicio responde `200` con `contentHash`, `ipfsCid` e `ipfsGatewayUrl` no vacíos

#### Scenario: Sin configuración IPFS

- **WHEN** `ContentAnchor:Enabled=false` o faltan credenciales Pinata
- **THEN** el servicio responde `503` con Problem Details `error=ipfs_not_configured`

#### Scenario: Emisor sin membership institucional

- **WHEN** un JWT válido sin membership para `{institutionId}` solicita anclaje
- **THEN** el servicio responde `403` con `error=forbidden`

#### Scenario: Documento inválido para JCS

- **WHEN** el body no contiene un `document` JSON objeto o array top-level válido
- **THEN** el servicio responde `400` con `error=invalid_anchor_document`

### Requirement: Canonicalización JCS RFC 8785

El módulo `CredentialContentAnchor` SHALL serializar el `document` usando JSON Canonicalization Scheme (RFC 8785) antes de calcular hash o pin.

El `contentHash` SHALL calcularse sobre los bytes UTF-8 exactos producidos por la canonicalización JCS.

El adaptador de pinning SHALL subir esos mismos bytes (no una re-serialización distinta del objeto).

#### Scenario: Hash determinista para el mismo documento

- **WHEN** se ancla dos veces el mismo `document` semánticamente equivalente
- **THEN** ambas respuestas producen el mismo `contentHash`

#### Scenario: Bytes pinneados recuperables por gateway

- **WHEN** un anclaje exitoso devuelve `ipfsGatewayUrl`
- **THEN** una descarga HTTP GET a esa URL devuelve bytes cuyo SHA-256 coincide con `contentHash`

### Requirement: Adaptador Pinata con pinFileToIPFS

Cuando el adaptador de producción está activo, el sistema SHALL autenticarse contra Pinata con API Key y API Secret (no JWT de navegador) y SHALL usar la API `pinFileToIPFS` con los bytes canónicos JCS.

#### Scenario: Fallo de Pinata

- **WHEN** Pinata responde con error HTTP o timeout
- **THEN** el servicio responde `502` con `error=content_anchor_failed`

### Requirement: Configuración ContentAnchor

El servicio SHALL leer configuración `Issuer:ContentAnchor` con al menos:

- `Enabled` (bool)
- `VerifyEnabled` (bool) — usado por verificación en link (capability `issuer-title-content-verification`)
- `GatewayBase` (string URL base del gateway, sin CID)
- `PinataApiKey`, `PinataApiSecret` (strings)
- `IpfsTimeoutSeconds` (int, default 30)

En `appsettings.json` base, `Enabled` y `VerifyEnabled` SHALL default `false`.

#### Scenario: Gateway configurable por entorno

- **WHEN** `GatewayBase` es `https://gateway.pinata.cloud/ipfs` en staging
- **THEN** `ipfsGatewayUrl` en la respuesta usa ese prefijo con el CID devuelto

### Requirement: Contrato OpenAPI y BFF

El snapshot `docs/contracts/issuer.openapi.json` SHALL documentar la operación de anclaje con request/response tipados y seguridad Bearer.

El BFF SHALL exponer pass-through `POST /api/issuer/institutions/{institutionId}/documents/anchor` hacia issuer-api reenviando `Authorization`.

#### Scenario: Snapshot verificable en CI

- **WHEN** se ejecuta `verify-openapi` para issuer
- **THEN** el snapshot incluye la nueva operación sin drift respecto al servicio vivo
