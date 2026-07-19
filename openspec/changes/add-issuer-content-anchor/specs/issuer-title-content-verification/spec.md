## ADDED Requirements

### Requirement: Verificación de contenido IPFS al vincular título

Cuando `Issuer:ContentAnchor:VerifyEnabled=true`, `IssuerService.LinkStudentTitleAsync` SHALL verificar que el `ipfsGatewayUrl` del request es alcanzable y que el SHA-256 de los bytes descargados coincide con `contentHash` del request antes de persistir en `credentials`.

Cuando `VerifyEnabled=false`, el sistema SHALL usar `NullContentAnchorVerifier` y SHALL NOT realizar descargas HTTP (comportamiento actual de solo validar strings no vacíos se mantiene como mínimo).

Si la verificación está habilitada y falla, el servicio SHALL responder `409` con `error=content_anchor_invalid` y MUST NOT insertar la fila en `credentials`.

#### Scenario: Contenido coherente en link

- **WHEN** `VerifyEnabled=true` y el gateway devuelve bytes cuyo hash coincide con `contentHash`
- **THEN** el vínculo de título procede normalmente (`201`)

#### Scenario: Hash no coincide

- **WHEN** `VerifyEnabled=true` y el gateway responde bytes con hash distinto
- **THEN** el servicio responde `409` con `error=content_anchor_invalid`

#### Scenario: Gateway no alcanzable

- **WHEN** `VerifyEnabled=true` y el gateway no responde dentro del timeout
- **THEN** el servicio responde `409` con `error=content_anchor_invalid`

#### Scenario: Verificación deshabilitada

- **WHEN** `VerifyEnabled=false`
- **THEN** el vínculo de título no descarga el gateway (comportamiento compatible con despliegues sin verify)

### Requirement: Persistencia de DIDs sin cambio de contrato

`LinkStudentTitleAsync` SHALL seguir derivando `subject_did` desde `student_wallets.did` e `issuer_did` desde `institutions.did` — el request de título MUST NOT incluir campos DID.

Si `institutions.did` es nulo o vacío, el vínculo SHALL fallar con `409 title_link_failed` (sin cambio respecto al comportamiento actual).

#### Scenario: Institución sin DID emisor

- **WHEN** la institución tiene wallet emisora pero `did` NULL en BD
- **THEN** el servicio responde `409` con `error=title_link_failed`
