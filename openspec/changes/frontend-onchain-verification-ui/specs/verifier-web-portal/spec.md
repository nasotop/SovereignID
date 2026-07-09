## MODIFIED Requirements

### Requirement: Render del veredicto de verificación

El portal SHALL renderizar el resultado de una verificación exitosa (`200 OK`) delegando la presentación a `VerificationVerdictPanel` con preset `verifierFull`.

El portal SHALL mostrar el `result` como veredicto resumido distinguible (válida / revocada / expirada / inexistente / **integridad comprometida**), la lista de `checks` agrupada (registro local / evidencia on-chain), fuentes `validationSource` y `revocationSource` cuando aplique, y el bloque `credential` con anclas vía `CredentialAnchorsPanel`.

Los `checks` con valor `null` SHALL mostrarse como «No evaluado», diferenciados de `true`/`false`.

Cuando la evidencia on-chain/IPFS está deshabilitada por configuración (todos los checks de evidencia `null` y `validationSource = not_evaluated`), el portal SHALL mostrar un banner informativo.

Cuando `result` es `not_found`, el portal SHALL indicar que la credencial no existe y MUST NOT intentar renderizar el bloque `credential`.

#### Scenario: Credencial válida renderizada con panel compartido

- **WHEN** el backend responde `200` con `result = valid` y bloque `credential` poblado
- **THEN** el portal muestra `VerificationVerdictPanel` con veredicto de credencial válida
- **AND** muestra checks agrupados y anclas vía `CredentialAnchorsPanel`

#### Scenario: Integridad comprometida renderizada

- **WHEN** el backend responde `200` con `result = integrity_failed`
- **THEN** el portal muestra badge «Integridad comprometida»
- **AND** muestra los checks de evidencia evaluados

#### Scenario: Credencial inexistente renderizada

- **WHEN** el backend responde `200` con `result = not_found` y `credential = null`
- **THEN** el portal indica que la credencial no existe
- **AND** no renderiza un bloque `credential`

### Requirement: Manejo de errores de protocolo vía seam de Problem Details

Cuando el backend responde un error de protocolo (`400` o `429` Problem Details), el portal SHALL mostrar al usuario el `detail` extraído por el seam único de errores (incluyendo `RateLimitExceededError` desde `VerifierService`), sin que el componente parsee el cuerpo del error.

#### Scenario: credentialId rechazado por el backend

- **WHEN** el backend responde `400` con Problem Details y `error = invalid_credential_id`
- **THEN** el portal muestra un estado de error con el mensaje `detail` del Problem Details
- **AND** ofrece reintentar la verificación

#### Scenario: Rate limit excedido

- **WHEN** el backend responde `429` con Problem Details y `error = rate_limit_exceeded`
- **THEN** el portal muestra mensaje de límite de tasa al usuario
- **AND** ofrece reintentar la verificación

## ADDED Requirements

### Requirement: Pre-relleno de credentialId desde query param

El portal verifier SHALL leer el query param `credentialId` al montar y, si es un UUID válido, pre-rellenar el campo de entrada.

El portal MUST NOT ejecutar verificación automáticamente al cargar con query param.

#### Scenario: Navegación desde holder con UUID

- **WHEN** el usuario navega a `/verifier?credentialId={uuid-válido}`
- **THEN** el campo de entrada muestra el UUID
- **AND** la verificación no se dispara hasta que el usuario pulse el control de verificar

#### Scenario: Query param inválido ignorado

- **WHEN** el usuario navega a `/verifier?credentialId=not-a-uuid`
- **THEN** el campo de entrada queda vacío o con validación de formato inválido
- **AND** no se invoca al backend automáticamente
