# holder-web-portal Specification

## Purpose

Portal web autenticado del titular (`/holder`): listado de credenciales vía cliente generado desde OpenAPI issuer, fachada `HolderService`, proxy nginx `/issuer/`, y manejo de errores vía Problem Details.

## Requirements

### Requirement: Portal holder autenticado con SIWE

El portal web `holder` (`/holder`) SHALL requerir sesión SIWE activa (guard de ruta existente) y SHALL cargar las credenciales del titular autenticado al montar el componente invocando `GET /issuer/holders/me/credentials` con el JWT de sesión.

El portal MUST NOT mostrar datos mock estáticos una vez implementado este cambio.

#### Scenario: Carga exitosa de credenciales

- **WHEN** un usuario autenticado navega a `/holder`
- **THEN** el portal invoca `GET /issuer/holders/me/credentials` con `Authorization: Bearer {jwt}`
- **AND** renderiza una tarjeta por cada credencial devuelta

#### Scenario: Titular sin credenciales

- **WHEN** el backend responde `200` con un arreglo vacío
- **THEN** el portal muestra un estado vacío indicando que no hay credenciales emitidas
- **AND** no muestra tarjetas mock

### Requirement: Consumo del backend mediante cliente generado a ruta relativa

El portal SHALL invocar los endpoints holder del servicio `issuer` a través del cliente HTTP generado desde `docs/contracts/issuer.openapi.json`, usando rutas relativas bajo `/issuer/` proxeadas por nginx hacia `issuer-api`.

El componente MUST hablar únicamente con una fachada de aplicación (`HolderService`) que envuelve el cliente generado; el componente MUST NOT leer `HttpErrorResponse` ni el cuerpo HTTP de errores directamente.

#### Scenario: Listado enrutado por el gateway

- **WHEN** el portal carga las credenciales del titular
- **THEN** realiza `GET` a la ruta relativa `/issuer/holders/me/credentials`
- **AND** procesa la respuesta a través de la fachada

### Requirement: Render de tarjetas de credencial

El portal SHALL renderizar cada credencial con: título (`title`), emisor (`issuerName`), fecha de emisión (`issuedAt` formateada), badge de `status` (active / revoked / expired con estilos distinguibles), e icono derivado de `typeCode` (`TITULO` → degree; otros → certificate).

#### Scenario: Credencial activa renderizada

- **WHEN** el backend devuelve una credencial con `status = active` y `typeCode = TITULO`
- **THEN** el portal muestra badge "Active" con estilo de credencial vigente
- **AND** muestra el icono de título (degree)

#### Scenario: Credencial revocada renderizada

- **WHEN** el backend devuelve una credencial con `status = revoked`
- **THEN** el portal muestra badge "Revoked" con estilo distinto al de active

### Requirement: Detalle y descarga JSON

El portal SHALL permitir descargar un archivo JSON con el detalle de una credencial invocando `GET /issuer/holders/me/credentials/{credentialId}` (o reutilizando detalle ya obtenido) y serializando la respuesta API (incluyendo `anchors` y `metadata` cuando existan).

#### Scenario: Download JSON exitoso

- **WHEN** el usuario pulsa "Download JSON" en una tarjeta
- **THEN** el portal obtiene el detalle de esa credencial
- **AND** inicia la descarga de un archivo `.json` con el payload del detalle

### Requirement: Compartir identificador de credencial

El portal SHALL permitir compartir el UUID de la credencial (`id`) para verificación externa (p. ej. portal verifier). En v1 MAY copiar el UUID al portapapeles o mostrarlo; MUST NOT requerir escáner de cámara.

#### Scenario: Copiar UUID para verificación

- **WHEN** el usuario pulsa "Share QR" (o control equivalente de compartir)
- **THEN** el UUID de la credencial queda disponible para el usuario (copiado o visible)
- **AND** ese UUID es el mismo `credentials.id` usable en `POST /verifications`

### Requirement: Manejo de errores vía seam de Problem Details

Cuando el backend responde error (`401`, `404`, u otro Problem Details), el portal SHALL mostrar al usuario el `detail` extraído por `error.utils.ts` y SHALL ofrecer reintento o redirección a login en caso de `401`.

#### Scenario: Sesión expirada

- **WHEN** el backend responde `401` con Problem Details
- **THEN** el portal muestra el mensaje `detail`
- **AND** permite volver a iniciar sesión

#### Scenario: Error de carga con reintento

- **WHEN** el backend responde un error distinto de autenticación durante la carga inicial
- **THEN** el portal muestra estado de error con `detail`
- **AND** ofrece reintentar la carga
