# ADR-0005: BFF .NET con clientes Kiota hacia microservicios

## Estado

Aceptado — 2026-07-01

**Complementa** [ADR-0004](0004-openapi-client-codegen.md): el frontend sigue generando su cliente con `ng-openapi-gen`, pero las rutas de verifier e issuer pasan por el BFF (`/api/`). Kiota queda en el backend como adapter hacia los snapshots downstream.

## Contexto

El portal web hablaba con cada microservicio vía nginx (`location` por prefijo: `/verifications`, `/issuer/`). Eso exponía la topología interna al navegador, duplicaba responsabilidades de gateway en nginx, y no dejaba un seam server-side para propagación JWT, composición futura ni evolución del contrato público.

ADR-0004 resolvió el drift de tipos en Angular con `ng-openapi-gen` directo desde snapshots de microservicio. Al crecer el número de servicios (`academy`, `identity`) y portales, se necesita una capa intermedia con profundidad real.

## Decisión

Introducir **`bff-api`** (`src/bff/`) como **Backend-for-Frontend**:

| Aspecto | Decisión |
|---------|----------|
| Interfaz pública | `Bff.Api` expone OpenAPI propio → `docs/contracts/bff.openapi.json` |
| Contrato v1 | **Pass-through**: mismas rutas y DTOs que los microservicios downstream |
| Prefijo browser | `/api/` en nginx con strip hacia `bff-api:8080` |
| Clientes downstream | **Kiota** generados desde `docs/contracts/{servicio}.openapi.json` → `Bff.Clients/Generated/` |
| Auth SIWE | **Fuera del BFF** en v1: `/auth/` sigue directo a `auth-api` |
| JWT holder (issuer) | **Solo reenvío** en v1: `AuthorizationForwardingHandler` copia el header; issuer valida |
| Regeneración Kiota | `scripts/gen-kiota-clients.ps1`; código commiteado (análogo a ADR-0004) |
| Frontend | `rootUrl = '/api'` en clientes verifier/issuer (`app.config.ts`) |

### Microservicios en Docker (v1)

| Servicio | En compose | Expuesto al browser |
|----------|------------|---------------------|
| `auth-api` | sí | sí (`/auth/`) |
| `bff-api` | sí | sí (vía `/api/`) |
| `verifier-api` | sí | no (solo red interna) |
| `issuer-api` | sí | no |
| `academy-api` | sí | no (vía BFF cuando se consuma) |
| `identity-api` | sí | no (scaffold; solo health) |

### Rutas BFF v1 (pass-through)

- `POST /verifications` → verifier
- `GET /issuer/holders/me/credentials` (+ detalle) → issuer (JWT reenviado)
- Rutas `academy/*` → academy
- `GET /health` → liveness del BFF

## Consecuencias

### Positivas

- Un seam server-side con profundidad: orquestación, agregación y políticas transversales tienen hogar en `Bff.Api` / futuro `Bff.Application`.
- El navegador deja de conocer la topología interna de microservicios (salvo auth SIWE, deliberado en v1).
- Kiota tipa las llamadas downstream; los snapshots OpenAPI siguen siendo fuente de verdad compartida con CI.
- `academy` e `identity` entran en compose sin nuevos `location` nginx.

### Negativas

- Nueva dependencia (`Microsoft.OpenApi.Kiota` CLI + `Microsoft.Kiota.Bundle`).
- Latencia extra (hop BFF) frente a proxy directo nginx → MS.
- Pass-through acopla el contrato BFF a la forma wire de los MS hasta evolucionar a DTOs agregados.
- Dos toolchains de codegen: Kiota (backend) + ng-openapi-gen (frontend).

## Referencias

- [ADR-0004](0004-openapi-client-codegen.md) — codegen frontend
- [ADR-0001](0001-problem-details-errors.md) — Problem Details reenviados sin reinterpretar
- [`scripts/gen-kiota-clients.ps1`](../../scripts/gen-kiota-clients.ps1)
- [`src/bff/`](../../src/bff/)
