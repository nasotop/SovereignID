## ADDED Requirements

### Requirement: Declaración de `result` como enum en el contrato OpenAPI publicado

El documento OpenAPI publicado del servicio `verifier` (`docs/contracts/verifier.openapi.json`) SHALL declarar la propiedad `result` de la respuesta de verificación como un `enum` cuyos valores son exactamente los emitidos en v1: `valid`, `revoked`, `expired`, `not_found`.

La declaración del `enum` MUST reflejar los valores estables en el formato del wire (snake_case) y MUST NOT alterar el valor que el backend emite en la respuesta HTTP. El snapshot versionado SHALL regenerarse tras el cambio para mantener la verificación de contrato en CI.

#### Scenario: El snapshot OpenAPI declara el enum de result

- **WHEN** se inspecciona el esquema de la respuesta de `POST /verifications` en `docs/contracts/verifier.openapi.json`
- **THEN** la propiedad `result` está declarada como `enum`
- **AND** el `enum` contiene exactamente `valid`, `revoked`, `expired`, `not_found`

#### Scenario: El valor emitido sigue siendo el del wire

- **WHEN** el backend responde una verificación con un veredicto cualquiera
- **THEN** el valor de `result` en la respuesta HTTP es el mismo valor snake_case que antes del cambio
- **AND** un cliente generado desde el contrato tipa `result` como la unión de los cuatro valores del enum
