## 1. Monorepo y documentación

- [x] 1.1 Crear `SovereignID.sln` en la raíz referenciando proyectos bajo `src/auth/` y `tests/auth/`
- [x] 1.2 Crear estructura `src/auth/{Auth.Api, Auth.Application, Auth.Domain, Auth.Infrastructure}` con referencias entre proyectos según design
- [x] 1.3 Migrar plantilla desde `src/Auth.API` a `src/auth/Auth.Api` (Program, appsettings, Dockerfile) y eliminar carpeta antigua
- [x] 1.4 Crear `tests/auth/Auth.IntegrationTests` (xUnit + `WebApplicationFactory`)
- [x] 1.5 Crear `CONTEXT.md` en raíz (mapa monorepo, servicio auth, glosario, desambiguación nombre repo legado)
- [x] 1.6 Limpiar `docs/siwe-backend-contract.md` (quitar §11, referencias legado/CONTEXT/réplica en encabezado y §12)

## 2. Dominio y aplicación

- [x] 2.1 Modelar `AuthChallenge` e invariantes en `Auth.Domain` (nonce, issued/expiry, consumed)
- [x] 2.2 Definir códigos de error de dominio y resultado tipado para verify en `Auth.Domain` / `Auth.Application`
- [x] 2.3 Implementar `IssueNonceUseCase` en `Auth.Application`
- [x] 2.4 Implementar `VerifySiweUseCase` con orden de validación contractual en `Auth.Application`
- [x] 2.5 Registrar `TimeProvider` inyectable para pruebas de caducidad

## 3. Infraestructura

- [x] 3.1 Implementar `InMemoryChallengeStore` (`ConcurrentDictionary`, consumo atómico, TTL 600s)
- [x] 3.2 Implementar `SiweMessageParser` (10 líneas obligatorias, opcionales EIP-4361, `OriginalPayload`, normalización `\r\n`)
- [x] 3.3 Implementar verificación de firma `personal_sign` / EIP-191 sobre `OriginalPayload`
- [x] 3.4 Implementar `JwtBearerTokenIssuer` (HS256, claims `sub`, `address`, `did`, `iat`, `exp`, `iss`, `aud`, TTL 24h)
- [x] 3.5 Validar `AUTH_JWT_SIGNING_KEY` al arranque fuera de Development (≥32 bytes UTF-8)
- [x] 3.6 Registrar implementaciones en DI desde `Auth.Api`

## 4. API HTTP

- [x] 4.1 Exponer `GET /auth/nonce` con contrato JSON del spec `auth-challenge`
- [x] 4.2 Exponer `POST /auth/verify` con contrato JSON del spec `auth-siwe-verify`
- [x] 4.3 Implementar filtro/middleware de Problem Details (`title`, `status`, `detail`, `error`) para códigos estables
- [x] 4.4 Configurar iss/aud/TTLs vía appsettings o variables de entorno

## 5. Pruebas de aceptación (AC-01…AC-07)

- [x] 5.1 Añadir helper de tests con Nethereum (`EthereumMessageSigner` + clave efímera) para construir mensaje SIWE válido Sepolia
- [x] 5.2 AC-01: happy path nonce → sign → verify → JWT `sub` en minúsculas
- [x] 5.3 AC-02: replay → `nonce_consumed`
- [x] 5.4 AC-03: nonce caducado con reloj fake → `nonce_expired`
- [x] 5.5 AC-04: chain incorrecta → `unsupported_chain`
- [x] 5.6 AC-05: mensaje alterado → `signature_mismatch`
- [x] 5.7 AC-06: nonce desconocido → `nonce_unknown`
- [x] 5.8 AC-07: payload malformado → `siwe_parse_failed` con `detail` legible

## 6. Cierre

- [x] 6.1 Verificar build de solución y ejecución de todos los tests de integración
- [x] 6.2 Actualizar `Auth.Api.http` o README del servicio auth con ejemplos de llamada (opcional si ya documentado en contrato)
