## Why

El portal `holder` (`/holder`) muestra credenciales con **datos mock** (`MOCK_HOLDER_CREDENTIALS`) y no llama al backend. El servicio `issuer` ya expone `GET /issuer/holders/me/credentials` y detalle autenticado con JWT SIWE, pero el front no tiene cliente HTTP, proxy nginx ni servicio Docker — el contrato OpenAPI tampoco documenta aún el esquema de seguridad Bearer ni enums tipados para el codegen. Hay que cablear el portal Holder al issuer real siguiendo el patrón ya aplicado en el portal verifier ([ADR-0004](../../../docs/adr/0004-openapi-client-codegen.md)).

## What Changes

- **Enriquecer el contrato OpenAPI de Issuer:** declarar `securitySchemes` Bearer JWT, aplicar `security` en los endpoints holder, y modelar `status` como `enum` (`active|revoked|expired`) en `HolderCredentialSummary`/`HolderCredentialDetail` para codegen tipado.
- **Reexportar y verificar** `docs/contracts/issuer.openapi.json` (`scripts/export-openapi.sh issuer` + CI `verify-openapi`).
- **Generar cliente HTTP del front** desde el snapshot con `ng-openapi-gen` → `src/web/src/app/api/issuer/`, código commiteado, script `gen:api:issuer`.
- **Fachada `HolderService`** (a mano) que envuelve el cliente generado, adjunta `Authorization: Bearer {jwt}` desde `AuthService`, y aplica el seam de Problem Details (`error.utils.ts`).
- **Reescribir `HolderComponent`:** cargar credenciales al iniciar (signals: loading / loaded / error / empty), renderizar tarjetas desde la API (badge `status`, icono por `typeCode`), estados de error con `detail`, botones Download JSON (detalle + `anchors`) y Share QR (UUID en crudo, sin escáner).
- **Infra de punta a punta:** `location /issuer/` en `nginx.conf`, servicio `issuer-api` en `docker-compose.yml`, `web depends_on: issuer-api`.

## Capabilities

### New Capabilities

- `holder-web-portal`: Portal web autenticado del titular: listado de credenciales vía cliente generado + fachada, render de tarjetas, detalle para download, manejo de errores vía seam, acceso al backend por ruta relativa `/issuer/`.
- `issuer-holder-credentials`: Contrato HTTP documentado para consultas holder en Issuer: OpenAPI con Bearer JWT, enums estables, snapshot versionado y verificable por CI.

### Modified Capabilities

- _(ninguna — no hay specs archivadas previas para issuer ni holder en `openspec/specs/`)_

## Impact

- **Backend (.NET):** `Issuer.Api/OpenApi/IssuerOpenApiExtensions.cs` — security scheme + enum transformer; sin cambio de semántica del wire.
- **Contrato:** `docs/contracts/issuer.openapi.json` (reexportar).
- **Frontend (web):** `holder.component.ts` (reescritura), nueva fachada `core/services/holder.service.ts`, cliente generado en `src/app/api/issuer/`, `package.json` (script `gen:api:issuer`), `ng-openapi-gen.issuer.json` (o config multi-servicio).
- **Infra:** `src/web/nginx.conf`, `docker-compose.yml` (+ override si aplica), `.env.example`.
- **Docs:** `CONTEXT.md`, `docs/issuer-domain-contract.md` (formalizar integración web).
- **Dependencias:** reutiliza `ng-openapi-gen` ya presente por verifier.
