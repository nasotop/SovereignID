## ADDED Requirements

### Requirement: Emisión consume anclaje backend

`TitleIssuanceService.issueTitle` SHALL anclar el documento VC llamando al endpoint BFF `POST /api/issuer/institutions/{institutionId}/documents/anchor` con el JSON-LD producido por `VcDocumentService`.

El servicio SHALL usar los valores `contentHash`, `ipfsCid` e `ipfsGatewayUrl` de la respuesta para los pasos on-chain y `linkStudentTitle` — MUST NOT calcular hash ni CID en el navegador.

Si el anclaje falla, el flujo SHALL abortar antes de invocar `CredentialContractService` (MetaMask).

#### Scenario: Emisión exitosa end-to-end

- **WHEN** el anchor responde `200` y MetaMask completa `registerCredential`
- **THEN** `linkStudentTitle` persiste la credencial y la UI muestra mensaje de éxito

#### Scenario: Anchor falla por IPFS no configurado

- **WHEN** el anchor responde `503` con `error=ipfs_not_configured`
- **THEN** la UI muestra error y no solicita firma MetaMask

#### Scenario: Anchor falla por error Pinata

- **WHEN** el anchor responde `502` con `error=content_anchor_failed`
- **THEN** la UI muestra error y no solicita firma MetaMask

### Requirement: Eliminación de pinning legacy en browser

El repositorio SHALL NOT incluir `IpfsPinningService` ni lecturas de `localStorage.sovereignid.pinata.jwt` tras este cambio.

#### Scenario: Sin referencias a CID fake

- **WHEN** se busca el patrón `bafy` + sufijo `dev` en servicios de emisión
- **THEN** no hay generación de CID determinístico fake en el frontend

### Requirement: VcDocumentService permanece en frontend

La construcción del VC JSON-LD (`issuer`, `credentialSubject`, `@context`, etc.) SHALL seguir en `VcDocumentService`; el backend recibe el documento ya armado.

#### Scenario: Documento enviado al anchor coincide con preview

- **WHEN** el emisor confirma emisión en el modal
- **THEN** el `document` POSTeado al anchor es el output de `buildTitleCredential` para el modelo del formulario

### Requirement: Cliente BFF regenerado

Tras el cambio backend, el proyecto SHALL regenerar `src/web/src/app/api/bff` para incluir la operación de anclaje con tipos alineados al snapshot OpenAPI.

#### Scenario: Tipos TypeScript del anchor disponibles

- **WHEN** se compila `npm run build` en `src/web`
- **THEN** no hay errores de tipo en la llamada al endpoint de anclaje
