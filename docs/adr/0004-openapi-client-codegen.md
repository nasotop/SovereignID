# ADR-0004: Cliente HTTP del frontend generado desde OpenAPI (ng-openapi-gen)

## Estado

Aceptado — 2026-06-25

**Revierte** la decisión "**alineación manual, sin codegen automático**" registrada en [`CONTEXT.md`](../../CONTEXT.md) (sección web del servicio `auth`). El resto de esa sección (seam único de errores en `error.utils.ts`, los componentes no leen el cuerpo HTTP directamente, snapshot OpenAPI versionado como fuente de verdad) sigue vigente y se refuerza.

## Contexto

Hasta ahora los tipos de request/response y los servicios HTTP del frontend (`auth.models.ts`, `auth-api.service.ts`) se escribían **a mano**, alineados con el snapshot OpenAPI y validados por CI vía fixtures. Al cablear el portal `verifier` (que no tenía ninguna capa de comunicación: el componente era una plantilla estática) se evaluó mantener ese patrón manual frente a generar el cliente desde el contrato.

El contrato del verifier (`docs/contracts/verifier.openapi.json`) es la fuente de verdad del backend (`Microsoft.AspNetCore.OpenApi`). Mantener un segundo juego de tipos a mano duplica el contrato y abre espacio a *drift* silencioso entre el JSON real y los tipos del cliente — justo el síntoma que motivó esta alineación.

## Decisión

El cliente HTTP del frontend (modelos + servicios) se **genera desde el snapshot OpenAPI** con **`ng-openapi-gen`**.

| Aspecto | Antes | Ahora (ADR-0004) |
|---------|-------|------------------|
| Tipos de request/response | Interfaces escritas a mano (`core/models`) | Generados desde OpenAPI (`app/api/<servicio>/models`) |
| Servicio HTTP | `@Service()` + `HttpClient` + `firstValueFrom` a mano | `Injectable` generado (`BaseService` + `ApiConfiguration.rootUrl`) |
| Fuente de verdad de tipos | Snapshot OpenAPI + tipos manuales | Snapshot OpenAPI (único) |
| Generación | — | `npm run gen:api:<servicio>`, **código generado commiteado** |

### Reglas concretas

- **Input:** el snapshot versionado `docs/contracts/<servicio>.openapi.json` (host-independiente). No se genera desde el servicio vivo.
- **Output por servicio:** `src/web/src/app/api/<servicio>/` (p. ej. `api/verifier/`). Cada microservicio genera su propio cliente; no se comparten tipos.
- **`rootUrl` vacío:** las llamadas salen como URLs relativas (`/verifications`), proxeadas por nginx hacia el `*-api` correspondiente — mismo modelo de gateway que `auth`.
- **Código generado commiteado:** espejo de la filosofía "snapshot commiteado + CI lo verifica". CI puede regenerar y diffear para detectar drift; el build no depende del codegen.
- **Seam de errores intacto:** una **fachada escrita a mano** (`VerifierService`) envuelve el servicio generado y aplica `error.utils.ts` (Problem Details). Los componentes hablan solo con la fachada, nunca con `HttpErrorResponse` crudo del servicio generado.
- **Enums en el contrato:** para que el cliente generado tenga tipos útiles, los valores estables del dominio se modelan como `enum` en el OpenAPI (p. ej. `result` ∈ `valid|revoked|expired|not_found`), no como `string` libre.

### Alcance y migración

- **`verifier`:** adopta el cliente generado desde su primera integración web.
- **`auth`:** permanece con tipos/servicio **manuales** por ahora; se migrará a `ng-openapi-gen` en un cambio posterior. Durante la transición conviven los dos estilos de forma deliberada.

## Consecuencias

### Positivas

- Un solo origen para los tipos del cliente: el contrato OpenAPI. Elimina el *drift* tipo manual ↔ wire.
- El enriquecimiento del contrato (enums) beneficia a cualquier consumidor del OpenAPI, no solo al front.

### Negativas

- Nueva dependencia de tooling (`ng-openapi-gen`) y un paso de generación a documentar/automatizar.
- Conviven temporalmente dos estilos (auth manual, verifier generado) hasta completar la migración de auth.
- El cliente generado no aplica por sí mismo el seam de Problem Details: requiere la fachada a mano para no romper la regla de [ADR-0001](0001-problem-details-errors.md).

## Referencias

- [`CONTEXT.md`](../../CONTEXT.md) — sección web; decisión de codegen y seam de errores
- [ADR-0001](0001-problem-details-errors.md) — Problem Details (el seam que la fachada preserva)
- [`docs/contracts/verifier.openapi.json`](../contracts/verifier.openapi.json) — contrato fuente del cliente generado
- [`docs/verifier-backend-contract.md`](../verifier-backend-contract.md) — semántica del veredicto y enum `result`
