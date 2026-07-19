## Why

El cambio backend `add-issuer-content-anchor` mueve el anclaje IPFS a issuer-api con JCS y fail-hard sin Pinata. El portal Angular sigue usando `IpfsPinningService` con CID fake y JWT en `localStorage`, lo que contradice el nuevo contrato y rompe verificación (`hashMatches`) en demos. Hay que consumir el endpoint BFF de anclaje y eliminar el código legacy del navegador.

**Dependencia:** requiere `add-issuer-content-anchor` desplegado o disponible en el entorno local (issuer + BFF con `POST .../documents/anchor`).

## What Changes

- `TitleIssuanceService` llama **`POST /api/issuer/institutions/{institutionId}/documents/anchor`** antes de MetaMask; ya no calcula hash ni pinnea localmente.
- Nuevo método en **`IssuerApiService`** (o cliente generado ng-openapi) para el anchor.
- **Eliminar** `IpfsPinningService` y referencias a `localStorage.sovereignid.pinata.jwt`.
- **`VcDocumentService`** se mantiene en frontend (construye VC); solo cambia quién ancla.
- Manejo de errores en UI emisor: `ipfs_not_configured` (503), `content_anchor_failed` (502) con mensajes claros en `IssuerTabComponent`.
- Regenerar cliente Angular **`src/web/src/app/api/bff`** desde snapshot BFF actualizado.
- Documentar en **`docs/deployment.md`** que Pinata se configura solo en issuer-api (no en browser).

## Capabilities

### New Capabilities

- `issuer-web-content-anchor`: integración del portal emisor con el endpoint de anclaje backend; refactor de `TitleIssuanceService`; eliminación de pinning legacy.

### Modified Capabilities

<!-- Sin deltas en specs archivadas de holder/verifier: el contrato HTTP de emisión no tenía spec archivada de UI. -->

## Impact

- **Frontend (`src/web/`):** `title-issuance.service.ts`, `issuer-api.service.ts`, eliminar `ipfs-pinning.service.ts`, posible ajuste `issuer-tab.component.ts` (mensajes error).
- **Sin cambios backend** en este cambio (solo consumo del API ya expuesto).
- **Tests:** actualizar/añadir tests de `TitleIssuanceService` si existen; smoke manual emisión end-to-end.
- **Docs:** `docs/deployment.md`, nota en `CONTEXT.md` sobre flujo de emisión actualizado.
