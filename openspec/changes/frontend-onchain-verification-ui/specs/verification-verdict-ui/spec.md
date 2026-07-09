## ADDED Requirements

### Requirement: Módulo VerificationVerdictPanel con preset verifierFull

El frontend SHALL implementar `VerificationVerdictPanel` en `shared/ui/verification-verdict/` como módulo de presentación del veredicto de `POST /verifications`.

El módulo SHALL aceptar `VerificationResponse` del cliente BFF generado y un preset `verifierFull` como configuración por defecto.

El módulo SHALL renderizar: badge de `result` (incluyendo `integrity_failed`), checks agrupados en secciones «Registro local» y «Evidencia on-chain», fuentes `validationSource` y `revocationSource` (esta última solo cuando `result = revoked`), banner cuando la evidencia está deshabilitada, y bloque `credential` componiendo `CredentialAnchorsPanel` para anclas.

Los portales MUST NOT duplicar labels, formateo ni layout de checks del veredicto fuera de este módulo.

#### Scenario: Veredicto válido con evidencia deshabilitada

- **WHEN** el panel recibe `result = valid` y los tres checks de evidencia (`onChainExists`, `hashMatches`, `signatureValid`) son `null` con `validationSource = not_evaluated`
- **THEN** el panel muestra el badge de credencial válida
- **AND** muestra un banner indicando que la verificación on-chain/IPFS no está habilitada en el entorno
- **AND** agrupa los checks de registro y evidencia en secciones separadas con valores «No evaluado» en evidencia

#### Scenario: Revocación con fuente on-chain

- **WHEN** el panel recibe `result = revoked` y `revocationSource = on_chain`
- **THEN** el panel muestra el badge de credencial revocada
- **AND** muestra la fila «Fuente de revocación» con etiqueta «On-chain»

#### Scenario: Integridad comprometida

- **WHEN** el panel recibe `result = integrity_failed`
- **THEN** el panel muestra badge «Integridad comprometida» con estilo distinguible de otros veredictos
- **AND** muestra los checks evaluados con Sí/No según corresponda

#### Scenario: Sin revocación oculta fuente de revocación

- **WHEN** el panel recibe `result = valid` o `expired` o `not_found`
- **THEN** el panel MUST NOT mostrar la fila `revocationSource`

### Requirement: Builder interno testeable buildVerdictViewModel

El módulo SHALL implementar `buildVerdictViewModel()` como función pura privada (exportada solo para tests) que concentra reglas de agrupación, visibilidad condicional y mensajes del banner.

El frontend SHALL incluir tests unitarios del builder cubriendo al menos: revocación con fuente visible, revocación oculta cuando no aplica, banner de evidencia deshabilitada, y formateo de checks `null`.

#### Scenario: Test de banner evidencia deshabilitada

- **WHEN** `buildVerdictViewModel` recibe una respuesta con evidencia toda `null` y `validationSource = not_evaluated`
- **THEN** el view model incluye `evidenceBanner.visible = true`
