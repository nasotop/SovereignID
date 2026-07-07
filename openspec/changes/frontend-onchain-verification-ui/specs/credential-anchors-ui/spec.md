## ADDED Requirements

### Requirement: Módulo CredentialAnchorsPanel para anclas verificables

El frontend SHALL implementar `CredentialAnchorsPanel` en `shared/ui/credential-anchors/` como módulo de presentación de anclas on-chain/IPFS (`ipfsCid`, `contentHash`, `transactionHash`, `chainId`, `blockNumber`, `eip712Signature`, `ipfsGatewayUrl`).

El panel SHALL usar `app-copy-value` para valores copiables, SHALL ofrecer enlace al gateway IPFS cuando `ipfsGatewayUrl` esté presente, y SHALL ofrecer enlace al block explorer **solo** cuando `chainId` corresponde a Sepolia (`11155111`).

Cuando `chainId` no es Sepolia, el panel SHALL mostrar `transactionHash` copiable sin enlace a explorer.

#### Scenario: Anclas Sepolia con explorer

- **WHEN** el panel recibe `chainId = 11155111` y un `transactionHash` válido
- **THEN** el panel muestra enlace al block explorer de Sepolia para la transacción
- **AND** permite copiar el hash vía `copy-value`

#### Scenario: Red no Sepolia sin explorer

- **WHEN** el panel recibe `chainId` distinto de `11155111`
- **THEN** el panel muestra `transactionHash` copiable
- **AND** MUST NOT renderizar enlace a block explorer

### Requirement: CTA de verificación pública opcional

Cuando el panel recibe `credentialId` no nulo, SHALL mostrar control de navegación a `/verifier` con query param `credentialId`.

El panel MUST NOT disparar verificación automáticamente.

#### Scenario: Enlace al verifier desde holder

- **WHEN** el panel se renderiza en el modal de anclas del holder con `credentialId` poblado
- **THEN** el usuario puede navegar a `/verifier?credentialId={uuid}`
- **AND** la verificación no se ejecuta hasta acción explícita en el portal verifier
