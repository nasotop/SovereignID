## ADDED Requirements

### Requirement: Entrada de verificación por UUID en el portal web

El portal web `verifier` SHALL ofrecer un campo de texto para introducir el `credentialId` (UUID) de la credencial a verificar y un control para disparar la verificación. El portal MUST NOT requerir autenticación.

El portal SHALL validar en cliente que el valor introducido tiene formato de UUID antes de invocar al backend, y SHALL deshabilitar o bloquear el envío cuando el valor está vacío o no es un UUID válido, mostrando un mensaje de validación.

El portal MUST NOT aceptar la subida de una Verifiable Credential completa (`.json`) ni un IPFS CID como entrada en v1.

#### Scenario: UUID válido habilita la verificación

- **WHEN** el usuario introduce un UUID con formato válido en el campo `credentialId`
- **THEN** el control de verificación queda habilitado
- **AND** al activarlo se invoca la verificación contra el backend

#### Scenario: UUID inválido bloqueado en cliente

- **WHEN** el usuario introduce un valor vacío o con formato que no es UUID
- **THEN** el portal no invoca al backend
- **AND** muestra un mensaje de validación indicando que el `credentialId` no es válido

### Requirement: Consumo del backend mediante cliente generado a ruta relativa

El portal SHALL invocar `POST /verifications` a través del cliente HTTP generado desde el contrato OpenAPI, usando una **ruta relativa** (`/verifications`) para que el gateway la enrute al servicio `verifier-api`.

El componente del portal MUST hablar únicamente con una fachada de aplicación que envuelve el cliente generado; el componente MUST NOT leer el cuerpo HTTP ni `HttpErrorResponse` directamente.

#### Scenario: Verificación enrutada por el gateway

- **WHEN** el portal dispara una verificación con un `credentialId` válido
- **THEN** se realiza una petición `POST` a la ruta relativa `/verifications` con el cuerpo `{ "credentialId": "<uuid>" }`
- **AND** la respuesta del backend se procesa a través de la fachada

### Requirement: Render del veredicto de verificación

El portal SHALL renderizar el resultado de una verificación exitosa (`200 OK`) mostrando: el `result` como veredicto resumido distinguible (válida / revocada / expirada / inexistente), la lista de `checks`, y el bloque `credential` cuando esté presente (incluyendo emisor, fechas y anclas).

Los `checks` con valor `null` SHALL mostrarse como "no evaluado" (reservado en v1), diferenciados de los `true`/`false`.

Cuando `result` es `not_found`, el portal SHALL indicar que la credencial no existe y MUST NOT intentar renderizar el bloque `credential`.

#### Scenario: Credencial válida renderizada

- **WHEN** el backend responde `200` con `result = valid` y bloque `credential` poblado
- **THEN** el portal muestra el veredicto de credencial válida
- **AND** muestra `checks.found/notRevoked/notExpired` como verdaderos y los chequeos externos como "no evaluado"
- **AND** muestra los datos de `credential` (tipo, estado, emisor, fechas, anclas)

#### Scenario: Credencial inexistente renderizada

- **WHEN** el backend responde `200` con `result = not_found` y `credential = null`
- **THEN** el portal indica que la credencial no existe
- **AND** no renderiza un bloque `credential`

### Requirement: Manejo de errores de protocolo vía seam de Problem Details

Cuando el backend responde un error de protocolo (`400` Problem Details), el portal SHALL mostrar al usuario el `detail` extraído por el seam único de errores, sin que el componente parsee el cuerpo del error.

#### Scenario: credentialId rechazado por el backend

- **WHEN** el backend responde `400` con Problem Details y `error = invalid_credential_id`
- **THEN** el portal muestra un estado de error con el mensaje `detail` del Problem Details
- **AND** ofrece reintentar la verificación
