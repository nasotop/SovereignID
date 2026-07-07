## MODIFIED Requirements

### Requirement: Declaración de `result` como enum en el contrato OpenAPI publicado

El documento OpenAPI publicado del servicio `verifier` (`docs/contracts/verifier.openapi.json`) SHALL declarar la propiedad `result` de la respuesta de verificación como un `enum` cuyos valores son exactamente: `valid`, `revoked`, `expired`, `not_found`, `integrity_failed`.

La declaración del `enum` MUST reflejar los valores estables en el formato del wire (snake_case) y MUST NOT alterar el valor que el backend emite en la respuesta HTTP para los cuatro valores preexistentes. El snapshot versionado SHALL regenerarse tras el cambio para mantener la verificación de contrato en CI.

#### Scenario: El snapshot OpenAPI declara el enum de result con el quinto valor

- **WHEN** se inspecciona el esquema de la respuesta de `POST /verifications` en `docs/contracts/verifier.openapi.json`
- **THEN** la propiedad `result` está declarada como `enum`
- **AND** el `enum` contiene exactamente `valid`, `revoked`, `expired`, `not_found`, `integrity_failed`

#### Scenario: El valor emitido sigue siendo el del wire

- **WHEN** el backend responde una verificación con un veredicto cualquiera
- **THEN** el valor de `result` en la respuesta HTTP es el mismo valor snake_case que antes del cambio para los casos `valid`, `revoked`, `expired`, `not_found`
- **AND** un cliente generado desde el contrato tipa `result` como la unión de los cinco valores del enum

### Requirement: Precedencia del veredicto de verificación

El caso de uso de verificación SHALL calcular `result` aplicando la siguiente precedencia, de mayor a menor prioridad: `not_found` > `revoked` > `expired` > `integrity_failed` > `valid`.

`integrity_failed` SHALL aplicarse únicamente cuando la credencial fue encontrada, no está expirada según BD/tiempo, y al menos uno de los chequeos de evidencia evaluados (`onChainExists`, `hashMatches`, `signatureValid`) devuelve `false`. Un chequeo en `null` (no evaluado) NUNCA MUST disparar `integrity_failed` por sí solo.

`revoked` SHALL tener prioridad sobre `integrity_failed`: una credencial revocada (por BD o por on-chain, ver `verifier-evidence-verification`) se reporta como `revoked` incluso si además tiene chequeos de evidencia en `false`.

`expired` SHALL tener prioridad sobre `integrity_failed`: una credencial vencida se reporta como `expired` incluso si además tiene chequeos de evidencia en `false`.

#### Scenario: Chequeo de evidencia falso sin revocación ni expiración produce integrity_failed

- **WHEN** una credencial existe, no está revocada, no está expirada, y `signatureValid` se evalúa como `false`
- **THEN** el `result` de la respuesta es `integrity_failed`

#### Scenario: Chequeos no evaluados no disparan integrity_failed

- **WHEN** una credencial existe, no está revocada, no está expirada, y todos los chequeos de evidencia (`onChainExists`, `hashMatches`, `signatureValid`) son `null`
- **THEN** el `result` de la respuesta es `valid`

#### Scenario: Revocación tiene prioridad sobre fallo de integridad

- **WHEN** una credencial está revocada (BD u on-chain) y además `hashMatches` se evalúa como `false`
- **THEN** el `result` de la respuesta es `revoked`

#### Scenario: Expiración tiene prioridad sobre fallo de integridad

- **WHEN** una credencial está expirada y además `onChainExists` se evalúa como `false`
- **THEN** el `result` de la respuesta es `expired`
