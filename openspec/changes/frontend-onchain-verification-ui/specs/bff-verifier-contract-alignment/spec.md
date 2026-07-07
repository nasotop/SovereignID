## ADDED Requirements

### Requirement: Nullable en validationSource y revocationSource del contrato BFF

El snapshot `docs/contracts/bff.openapi.json` SHALL declarar `validationSource` y `revocationSource` como `type: ["null", "string"]` con sus enums respectivos, manteniendo ambos campos en `required` (clave presente, valor puede ser `null`).

La fuente de generación OpenAPI en `Bff.Api` y `Verifier.Api` SHALL emitir el mismo schema nullable; el snapshot MUST NOT editarse a mano sin reexport.

Tras actualizar el contrato, el frontend SHALL regenerar el cliente con `npm run gen:api:bff` y los tipos TypeScript generados SHALL reflejar `| null` en ambos campos.

#### Scenario: Cliente generado con fuentes nullable

- **WHEN** se regenera el cliente BFF tras corregir el schema
- **THEN** `VerificationChecksResponse.validationSource` es unión de enum y `null`
- **AND** `VerificationChecksResponse.revocationSource` es unión de enum y `null`

### Requirement: Respuesta 429 documentada en BFF para verifications

El contrato `docs/contracts/bff.openapi.json` SHALL documentar respuesta `429` en `POST /verifications` con descripción de Problem Details `error = rate_limit_exceeded`, alineado con `verifier.openapi.json`.

El BFF SHALL declarar `[ProducesResponseType(StatusCodes.Status429TooManyRequests)]` en el controlador de verifications.

#### Scenario: Contrato BFF incluye rate limit

- **WHEN** se exporta el OpenAPI del BFF tras el cambio
- **THEN** `POST /verifications` lista respuesta `429` además de `200` y `400`

### Requirement: Error tipado rate_limit_exceeded en VerifierService

`VerifierService` SHALL mapear respuestas con `toErrorCode(error) === 'rate_limit_exceeded'` a `RateLimitExceededError` con mensaje legible para el usuario.

El portal verifier SHALL mostrar ese mensaje en estado de error sin parsear `HttpErrorResponse` en el componente.

#### Scenario: Rate limit en verificación

- **WHEN** el BFF devuelve `429` con Problem Details `error = rate_limit_exceeded`
- **THEN** `VerifierService.verifyCredential` lanza `RateLimitExceededError`
- **AND** el portal verifier muestra el mensaje al usuario con opción de reintentar
