## Why

El monorepo SovereignID necesita su primer microservicio deployable: autenticación con Sign-In with Ethereum (SIWE) según el contrato en `docs/siwe-backend-contract.md`. Hoy solo existe una plantilla Web API mal ubicada (`src/Auth.API`); no hay capas, ni flujo nonce→verify→JWT, ni pruebas de aceptación. Este cambio establece la estructura del monorepo por servicio (`src/auth/`) y entrega el servicio auth v1 listo para contenedor único.

## What Changes

- Reestructurar el repo: mover la plantilla a `src/auth/Auth.Api/` y crear proyectos `Auth.Application`, `Auth.Domain`, `Auth.Infrastructure`, más `tests/auth/Auth.IntegrationTests`.
- Añadir `SovereignID.sln` en la raíz del monorepo.
- Crear `CONTEXT.md` en la raíz (mapa del monorepo, glosario, desambiguación respecto al nombre de repo legado).
- Limpiar `docs/siwe-backend-contract.md`: eliminar referencias al monorepo antiguo, `CONTEXT.md` inexistente, §11 de implementación de referencia y lenguaje de “réplica”.
- Implementar `GET /auth/nonce` y `POST /auth/verify` con reglas de dominio, parser SIWE, verificación `personal_sign`, store de retos en memoria y emisión JWT HS256.
- Suite de integración con los siete escenarios AC-01…AC-07 del contrato.

## Capabilities

### New Capabilities

- `auth-challenge`: emisión y persistencia de auth challenges (nonce 32 hex, TTL 600s, un solo uso, errores `nonce_*`).
- `auth-siwe-message-parsing`: parseo línea a línea EIP-4361 y conservación de `OriginalPayload`.
- `auth-siwe-verify`: pipeline HTTP de verificación (orden de validación, chain Sepolia, firma, consumo de nonce).
- `auth-jwt-session`: emisión de JWT de sesión (claims, HS256, configuración `AUTH_JWT_SIGNING_KEY`).

### Modified Capabilities

- _(ninguna — no hay specs previas en `openspec/specs/`)_

## Impact

- **Código**: nuevo árbol bajo `src/auth/` y `tests/auth/`; eliminación de `src/Auth.API/` tras migración.
- **APIs**: endpoints públicos `/auth/nonce` y `/auth/verify`; errores RFC 7807 con códigos estables.
- **Dependencias**: Nethereum (tests; opcional en runtime para recuperación de firma), biblioteca JWT estándar .NET.
- **Configuración**: `AUTH_JWT_SIGNING_KEY`, iss/aud configurables, TTLs nonce/JWT según contrato.
- **Documentación**: `CONTEXT.md` nuevo; contrato SIWE actualizado para este monorepo independiente.
