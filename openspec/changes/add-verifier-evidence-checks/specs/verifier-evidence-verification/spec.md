## ADDED Requirements

### Requirement: Chequeo `onChainExists` exige existencia y coherencia completa

El sistema SHALL evaluar `onChainExists` consultando `getCredential(bytes32)` en el contrato `CredentialRegistry` mediante `eth_call`. El chequeo SHALL devolver `true` únicamente cuando el registro existe on-chain **y** sus campos `contentHash`, `ipfsCid`, `institutionId`, `issuer` y `subject` coinciden exactamente (comparación insensible a mayúsculas para direcciones) con los valores persistidos para esa credencial.

Un registro que existe on-chain pero cuyos campos no coinciden con los valores persistidos SHALL producir `onChainExists = false`, no `true`.

Cuando la llamada RPC falla o excede el tiempo de espera configurado, `onChainExists` SHALL ser `null`.

Este chequeo SHALL estar gobernado por el flag de configuración `OnChainCheckEnabled`; cuando está deshabilitado, `onChainExists` SHALL ser `null` sin intentar ninguna llamada RPC.

#### Scenario: Registro on-chain coherente

- **WHEN** `OnChainCheckEnabled` está habilitado y el contrato devuelve un registro cuyos campos coinciden exactamente con los de la credencial
- **THEN** `onChainExists` es `true`

#### Scenario: Registro on-chain existe pero es incoherente

- **WHEN** `OnChainCheckEnabled` está habilitado y el contrato devuelve un registro cuyo `contentHash` no coincide con el persistido
- **THEN** `onChainExists` es `false`

#### Scenario: Registro no existe on-chain

- **WHEN** `OnChainCheckEnabled` está habilitado y la llamada a `getCredential` revierte por credencial inexistente
- **THEN** `onChainExists` es `false`

#### Scenario: RPC no disponible

- **WHEN** `OnChainCheckEnabled` está habilitado y la llamada RPC falla por timeout o error de red
- **THEN** `onChainExists` es `null`

#### Scenario: Chequeo deshabilitado por configuración

- **WHEN** `OnChainCheckEnabled` está deshabilitado
- **THEN** `onChainExists` es `null` sin que el sistema realice ninguna llamada RPC

### Requirement: Chequeo `hashMatches` compara el contenido real de IPFS

El sistema SHALL evaluar `hashMatches` descargando el documento desde el `ipfs_gateway_url` persistido para la credencial, calculando SHA-256 sobre los bytes exactos de la respuesta, y comparando el resultado contra el `content_hash` persistido.

El chequeo SHALL usar un único gateway (el persistido por credencial), sin reintentar contra gateways alternativos. Ante timeout o error HTTP del gateway, `hashMatches` SHALL ser `null`. Ante una respuesta exitosa cuyo hash no coincide, `hashMatches` SHALL ser `false`.

Este chequeo SHALL estar gobernado por el flag `IpfsCheckEnabled`; cuando está deshabilitado, `hashMatches` SHALL ser `null` sin intentar ninguna descarga.

#### Scenario: Contenido IPFS coincide con el hash persistido

- **WHEN** `IpfsCheckEnabled` está habilitado y el gateway responde con el documento cuyo SHA-256 coincide con `content_hash`
- **THEN** `hashMatches` es `true`

#### Scenario: Contenido IPFS no coincide

- **WHEN** `IpfsCheckEnabled` está habilitado y el gateway responde exitosamente pero el SHA-256 calculado difiere de `content_hash`
- **THEN** `hashMatches` es `false`

#### Scenario: Gateway IPFS no disponible

- **WHEN** `IpfsCheckEnabled` está habilitado y la solicitud al gateway excede el timeout configurado o falla
- **THEN** `hashMatches` es `null`

#### Scenario: Chequeo deshabilitado por configuración

- **WHEN** `IpfsCheckEnabled` está deshabilitado
- **THEN** `hashMatches` es `null` sin que el sistema realice ninguna solicitud al gateway

### Requirement: Chequeo `signatureValid` con jerarquía de fuente asimétrica

El sistema SHALL evaluar `signatureValid` con la wallet emisora on-chain (`issuer` devuelto por `getCredential`) como fuente primaria, y con `institutions.issuer_wallet_address` (BD) como fallback exclusivamente cuando la fuente primaria no está disponible.

La fuente on-chain SHALL considerarse disponible únicamente cuando `onChainExists` es `true` (registro existente y coherente). Si el registro on-chain existe pero es incoherente, su campo `issuer` MUST NOT usarse para este chequeo; el sistema SHALL tratar la fuente on-chain como no disponible y aplicar el fallback.

El fallback a BD SHALL ser asimétrico:
- Si la firma recuperada coincide con `issuer_wallet_address` de BD, el sistema SHALL devolver `signatureValid = null` (inconcluso) — el fallback a BD MUST NOT producir una confirmación positiva por sí solo.
- Si la firma recuperada no coincide con `issuer_wallet_address` de BD, el sistema SHALL devolver `signatureValid = false` (rechazo válido incluso sin confirmación on-chain).
- Si BD tampoco tiene `issuer_wallet_address` configurada para la institución, el sistema SHALL devolver `signatureValid = null`.

Una firma ausente o malformada en los datos persistidos de la credencial SHALL producir `signatureValid = false` directamente, sin considerarse una falla de infraestructura.

Este chequeo SHALL estar gobernado por el flag `SignatureCheckEnabled`; cuando está deshabilitado, `signatureValid` SHALL ser `null` sin intentar ninguna verificación criptográfica.

#### Scenario: Firma válida confirmada contra emisor on-chain

- **WHEN** `SignatureCheckEnabled` está habilitado, `onChainExists` es `true`, y la firma EIP-712 recupera la wallet `issuer` del contrato
- **THEN** `signatureValid` es `true`
- **AND** `validationSource` es `on_chain`

#### Scenario: Firma inválida contra emisor on-chain

- **WHEN** `SignatureCheckEnabled` está habilitado, `onChainExists` es `true`, y la firma EIP-712 no recupera la wallet `issuer` del contrato
- **THEN** `signatureValid` es `false`
- **AND** `validationSource` es `on_chain`

#### Scenario: Registro on-chain incoherente no se usa como fuente de firma

- **WHEN** `SignatureCheckEnabled` está habilitado y `onChainExists` es `false` por incoherencia de campos (no por ausencia de RPC)
- **THEN** el sistema aplica el fallback a BD para `signatureValid`, sin usar el `issuer` del registro on-chain incoherente

#### Scenario: Fallback a BD coincide — resultado inconcluso, no confirmado

- **WHEN** `SignatureCheckEnabled` está habilitado, la fuente on-chain no está disponible, y la firma EIP-712 recupera una wallet que coincide con `institutions.issuer_wallet_address`
- **THEN** `signatureValid` es `null`
- **AND** `validationSource` es `bd_fallback_inconclusive`

#### Scenario: Fallback a BD no coincide — rechazo válido sin cadena

- **WHEN** `SignatureCheckEnabled` está habilitado, la fuente on-chain no está disponible, y la firma EIP-712 no recupera una wallet que coincida con `institutions.issuer_wallet_address`
- **THEN** `signatureValid` es `false`
- **AND** `validationSource` es `bd_fallback_rejected`

#### Scenario: Ni on-chain ni BD tienen datos de emisor disponibles

- **WHEN** `SignatureCheckEnabled` está habilitado, la fuente on-chain no está disponible, y la institución no tiene `issuer_wallet_address` configurada
- **THEN** `signatureValid` es `null`
- **AND** `validationSource` es `not_evaluated`

#### Scenario: Firma ausente es un dato corrupto, no una falla de infraestructura

- **WHEN** `SignatureCheckEnabled` está habilitado y la credencial no tiene firma EIP-712 persistida
- **THEN** `signatureValid` es `false`

#### Scenario: Chequeo deshabilitado por configuración

- **WHEN** `SignatureCheckEnabled` está deshabilitado
- **THEN** `signatureValid` es `null` sin que el sistema realice ninguna verificación criptográfica

### Requirement: Fuente de revocación trazable (`revocationSource`)

El sistema SHALL determinar `notRevoked` combinando el estado de BD y el estado on-chain: `notRevoked = !(bdRevoked || onChainRevoked)`. Una credencial marcada revocada on-chain SHALL producir `result = revoked` aunque BD indique estado activo. Una credencial marcada revocada en BD SHALL producir `result = revoked` aunque el estado on-chain no lo refleje.

El sistema SHALL exponer `revocationSource` en la respuesta con uno de los valores `bd`, `on_chain`, `both`, indicando qué fuente(s) reportaron la revocación.

#### Scenario: Revocada solo en BD

- **WHEN** BD marca la credencial como revocada y el chequeo on-chain (si se evaluó) no la marca revocada
- **THEN** `result` es `revoked`
- **AND** `revocationSource` es `bd`

#### Scenario: Revocada solo on-chain

- **WHEN** BD marca la credencial como activa y el contrato indica `revoked = true`
- **THEN** `result` es `revoked`
- **AND** `revocationSource` es `on_chain`

#### Scenario: Revocada en ambas fuentes

- **WHEN** BD marca la credencial como revocada y el contrato también indica `revoked = true`
- **THEN** `result` es `revoked`
- **AND** `revocationSource` es `both`

### Requirement: Trazabilidad persistida de los chequeos de evidencia

Cada intento de verificación registrado en `verification_logs` SHALL persistir los valores computados de `signature_valid`, `hash_matches`, `on_chain_exists`, además de las columnas nuevas `signature_validation_source` y `revocation_source`, reflejando exactamente los valores devueltos en la respuesta HTTP para esa verificación.

#### Scenario: Registro de auditoría completo

- **WHEN** se completa una verificación con todos los chequeos evaluados
- **THEN** la fila insertada en `verification_logs` contiene los mismos valores de `signature_valid`, `hash_matches`, `on_chain_exists`, `signature_validation_source` y `revocation_source` que la respuesta HTTP

### Requirement: Activación incremental de chequeos de evidencia por configuración

El sistema SHALL exponer tres flags de configuración independientes (`OnChainCheckEnabled`, `IpfsCheckEnabled`, `SignatureCheckEnabled`), cada uno `false` por defecto en todo entorno. Cada flag SHALL controlar únicamente su chequeo correspondiente, sin afectar a los otros dos.

#### Scenario: Un chequeo deshabilitado no afecta a los demás

- **WHEN** `IpfsCheckEnabled` está deshabilitado pero `OnChainCheckEnabled` y `SignatureCheckEnabled` están habilitados
- **THEN** `hashMatches` es `null` mientras `onChainExists` y `signatureValid` se evalúan normalmente

#### Scenario: Comportamiento v1 preservado por defecto

- **WHEN** ninguno de los tres flags fue configurado explícitamente
- **THEN** `onChainExists`, `hashMatches` y `signatureValid` son `null` para toda verificación, igual que el comportamiento anterior a este cambio
