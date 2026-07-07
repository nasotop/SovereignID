## ADDED Requirements

### Requirement: Límite de tasa por IP sobre el endpoint público de verificación

`Verifier.Api` SHALL aplicar un límite de tasa de tipo token bucket por dirección IP sobre `POST /verifications`, con capacidad de 10 tokens y recarga de 1 token cada 3 segundos. El límite SHALL aplicarse en el propio servicio `Verifier.Api`, no en el BFF.

Cuando una IP excede el límite, el sistema SHALL responder `429 Too Many Requests` con un cuerpo RFC 7807 Problem Details cuya extensión `error` sea `rate_limit_exceeded`.

#### Scenario: Requests dentro del límite se procesan normalmente

- **WHEN** una IP realiza hasta 10 verificaciones dentro de una ventana corta
- **THEN** todas las respuestas son procesadas normalmente según el resultado de la verificación

#### Scenario: Ráfaga que excede la capacidad es limitada

- **WHEN** una IP realiza más de 10 verificaciones sin dejar tiempo de recarga entre ellas
- **THEN** las solicitudes que exceden la capacidad disponible del bucket reciben `429 Too Many Requests`
- **AND** el cuerpo de la respuesta es Problem Details con `error = rate_limit_exceeded`

#### Scenario: El bucket se recarga con el tiempo

- **WHEN** una IP agota su bucket y luego espera al menos 3 segundos antes de la siguiente solicitud
- **THEN** dispone de al menos 1 token adicional disponible para esa solicitud

### Requirement: Resolución de IP real detrás de proxy interno

`Verifier.Api` SHALL resolver la dirección IP del cliente real usando `ForwardedHeadersMiddleware`, configurado con `KnownProxies`/`KnownNetworks` restringidos a la red interna conocida del stack (nginx y `bff-api`). El sistema MUST NOT confiar en encabezados `X-Forwarded-For` provenientes de orígenes fuera de esa red conocida.

#### Scenario: IP real resuelta a través del proxy interno

- **WHEN** una solicitud llega a `Verifier.Api` a través de nginx y `bff-api`, ambos dentro de la red interna configurada
- **THEN** el límite de tasa se aplica sobre la IP original del cliente, no sobre la IP del proxy interno

#### Scenario: Encabezado forwarded de origen no confiable se ignora

- **WHEN** una solicitud llega a `Verifier.Api` con un encabezado `X-Forwarded-For` pero no proviene de una red configurada como conocida
- **THEN** el sistema SHALL NOT usar ese encabezado para determinar la IP del cliente a efectos del límite de tasa
