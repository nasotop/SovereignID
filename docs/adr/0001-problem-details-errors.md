# ADR-0001: Problem Details como modelo transversal de errores HTTP

## Estado

Aceptado — 2026-06-21

## Contexto

El monorepo SovereignID expone varios microservicios HTTP consumidos por la SPA Angular (`src/web/`). Los errores de negocio deben ser legibles para humanos y programáticamente estables para el cliente.

Alternativas consideradas:

1. **Formato propio** con campo `message` (convención ad-hoc).
2. **RFC 7807 Problem Details** (`application/problem+json`) con extensión `error` para códigos estables.

El servicio `auth` ya implementaba Problem Details vía `AuthFailureExceptionFilter`. El frontend, sin embargo, buscaba `body.message`, por lo que los errores del backend no llegaban a la UI.

## Decisión

Todos los microservicios del monorepo responden errores de negocio con **RFC 7807 Problem Details**.

Forma observable:

| Campo | Obligatorio | Uso |
|-------|-------------|-----|
| `title` | sí | Resumen del tipo de error |
| `status` | sí | Código HTTP |
| `detail` | sí | Mensaje legible — **fuente para la UI en web** |
| `error` | sí | Código estable en `snake_case` (extensión) |

**Web** parsea Problem Details en un único seam (`error.utils.ts`). Los componentes no leen el cuerpo HTTP directamente.

**Backend** mapea fallos de dominio a Problem Details. Los catálogos de códigos `error` viven en el contrato de dominio de cada servicio (p. ej. `docs/siwe-backend-contract.md` para auth).

## Consecuencias

### Positivas

- Formato estándar, interoperable con herramientas y middleware ASP.NET (`AddProblemDetails`).
- Un solo parser en el frontend sirve para auth y futuros servicios.
- Separación clara: `detail` para humanos, `error` para lógica programática.

### Negativas

- Los desarrolladores acostumbrados a `{ "message": "..." }` deben adoptar `detail`.
- Cada nuevo servicio debe seguir el mismo filtro/patrón (no hay librería compartida aún en el monorepo).

## Referencias

- [RFC 7807 — Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807)
- [`CONTEXT.md`](../CONTEXT.md) — sección «Modelo de errores HTTP (transversal)»
- `src/auth/Auth.Api/AuthFailureExceptionFilter.cs` — implementación de referencia
