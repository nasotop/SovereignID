## ADDED Requirements

### Requirement: Inspección de anclas on-chain en modal

El portal holder SHALL ofrecer en cada tarjeta de credencial un control «Ver anclas» que abre un modal con `CredentialAnchorsPanel`.

Al abrir el modal, el portal SHALL obtener el detalle vía `GET /issuer/holders/me/credentials/{credentialId}` si no está en caché local, y SHALL reutilizar detalle cacheado para acciones posteriores (p. ej. descarga JSON).

El modal SHALL mostrar estado de carga mientras se obtiene el detalle.

#### Scenario: Apertura de modal con fetch lazy

- **WHEN** el titular pulsa «Ver anclas» en una credencial sin detalle cacheado
- **THEN** el portal muestra indicador de carga en el modal
- **AND** tras respuesta exitosa renderiza `CredentialAnchorsPanel` con `anchors` del detalle

#### Scenario: Reutilización de caché

- **WHEN** el titular ya descargó JSON o abrió anclas de la misma credencial en la sesión
- **THEN** el portal MUST NOT repetir la petición HTTP de detalle
- **AND** muestra anclas inmediatamente desde caché local

### Requirement: Navegación al verifier desde anclas

El modal de anclas SHALL incluir CTA que navega a `/verifier?credentialId={id}` usando el UUID de la credencial.

#### Scenario: Ir a verificar desde holder

- **WHEN** el titular pulsa el CTA de verificación en el modal de anclas
- **THEN** navega a `/verifier` con `credentialId` en query string
- **AND** el portal verifier pre-rellena el UUID sin auto-verificar

### Requirement: Limpieza de caché de detalle en logout

Al cerrar sesión desde el portal holder, el componente SHALL limpiar la caché local de detalles de credencial y cerrar el modal de anclas si está abierto.

#### Scenario: Logout limpia estado de anclas

- **WHEN** el titular pulsa logout con modal de anclas abierto o detalle cacheado
- **THEN** la caché de detalle queda vacía
- **AND** el modal de anclas se cierra
