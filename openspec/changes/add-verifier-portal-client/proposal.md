## Why

El portal `verifier` de la web es hoy una plantilla **100% estática** (`VerifierComponent {}` sin lógica, sin `HttpClient`, sin modelos): nunca llama a `POST /verifications`, así que el front y el contrato del backend "no se comunican". Además la UI propone arrastrar un `.json` completo, lo que contradice el contrato v1 (entrada = UUID). Hay que cablear el portal al verifier real y, de paso, eliminar el riesgo de *drift* entre los tipos del cliente y el contrato OpenAPI generando el cliente HTTP desde el snapshot.

## What Changes

- Reemplazar el drag & drop por un **campo de texto de UUID** (`credentialId`) con validación en cliente; el escáner QR queda fuera de alcance (futuro), pero el modelo de entrada hacia el backend siempre es el UUID.
- **Generar el cliente HTTP del front** (modelos + servicio) desde `docs/contracts/verifier.openapi.json` con `ng-openapi-gen`, salida en `src/web/src/app/api/verifier/`, `rootUrl` vacío y código generado commiteado. **BREAKING (proceso):** revierte la política "alineación manual, sin codegen" (registrada en ADR-0004).
- Añadir una **fachada `VerifierService`** (a mano) que envuelve el servicio generado y aplica el seam de Problem Details (`error.utils.ts`); el componente solo habla con la fachada.
- Renderizar el veredicto completo en el componente: badge de `result`, lista de `checks` (los `null` como "no evaluado") y bloque `credential` (emisor, fechas, anclas), más estados de carga/error (incluido `invalid_credential_id`).
- **Enriquecer el contrato** para que el OpenAPI emita `result` como `enum` (`valid|revoked|expired|not_found`) y reexportar el snapshot, de modo que el cliente generado obtenga una unión tipada.
- **Transporte de punta a punta:** añadir `location /verifications` en `nginx.conf` (→ `verifier-api:8080`) y el servicio `verifier-api` en `docker-compose.yml` (Dockerfile ya existe).

## Capabilities

### New Capabilities

- `verifier-web-portal`: El portal web público de verificación: entrada de UUID con validación, consumo del cliente HTTP generado vía la fachada con seam de Problem Details, render del veredicto (`result` + `checks` + `credential`) y acceso al backend a través del gateway en ruta relativa `/verifications`.

### Modified Capabilities

- `verifier-credential-verification`: El documento OpenAPI publicado SHALL declarar `result` como `enum` con los valores estables de v1, para que los consumidores generen tipos de unión (hasta ahora el spec solo exigía los valores a nivel de dominio, no su declaración como `enum` en el contrato).

## Impact

- **Frontend (web):** `src/web/src/app/features/portals/verifier/verifier.component.ts` (reescritura), nueva fachada `core/services/verifier.service.ts`, cliente generado en `src/web/src/app/api/verifier/`, `package.json` (devDependency `ng-openapi-gen` + script `gen:api:verifier`), nuevo `ng-openapi-gen.json`.
- **Backend (.NET):** `Verifier.Api` — anotación de `result` como enum en el documento OpenAPI (schema transformer o enum con `JsonConverter` snake_case), sin alterar `ToWireValue` ni `VerificationResponse` en el wire.
- **Contrato:** reexportar `docs/contracts/verifier.openapi.json`.
- **Infra:** `src/web/nginx.conf` (nueva `location`), `docker-compose.yml` (servicio `verifier-api`).
- **Docs:** ya actualizados `CONTEXT.md` y `docs/adr/0004-openapi-client-codegen.md` (esta propuesta los formaliza).
- **Dependencias nuevas:** `ng-openapi-gen` (devDependency del front).
