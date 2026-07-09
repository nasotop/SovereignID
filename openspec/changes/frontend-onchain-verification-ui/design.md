## Context

PR #34 (`add-verifier-evidence-checks`) añadió lectura on-chain real en `Verifier.Api` (`getCredential`, IPFS, EIP-712) y nuevos campos en `VerificationResponse`. El frontend tiene cliente BFF regenerado y lógica inline en `verifier.component.ts`, pero:

- Los schema transformers OpenAPI emiten `validationSource`/`revocationSource` sin `null` pese a runtime nullable.
- `bff.openapi.json` no documenta `429`.
- La presentación del veredicto (~70 líneas) no es reutilizable ni agrupa checks BD vs evidencia.
- El titular no ve anclas on-chain en UI (solo en JSON descargado).

Sesión de diseño (`/codebase-design` + `/grilling`) cerró 16 decisiones; este documento las consolida.

## Goals / Non-Goals

**Goals:**

- Alinear contrato BFF ↔ verifier (nullable + 429) con reexport y regen de clientes.
- Extraer módulos profundos `VerificationVerdictPanel` y `CredentialAnchorsPanel` en `shared/ui/`.
- Refactorizar verifier y holder en una iteración; cerrar ciclo titular → verifier.
- Tests del builder `buildVerdictViewModel()`; docs (`CONTEXT.md`, `future-work.md`).

**Non-Goals:**

- Presets `holderCompact` / `issuerPreview` (solo scaffolding de `VerdictPresentation`).
- Caché en `HolderService`; maestro-detalle inline en holder.
- Tabla genérica de block explorers (solo Sepolia v1).
- Auto-verificar al cargar `/verifier?credentialId=`.
- Desglose `integrity_failed` en reportes Academy (R-I5).

## Decisions

### D1 — Contrato: fuente C# + reexport (no parche manual JSON)

**Decisión:** Corregir `BffOpenApiExtensions.cs` y `VerifierOpenApiExtensions.cs`; reexportar snapshots; regen Kiota + `ng-openapi-gen`.

**Alternativa descartada:** Parche manual de `bff.openapi.json` — drift en CI y violación ADR-0005.

### D2 — VerificationVerdictPanel: híbrido presets + builder privado

**Decisión:** Componente con `[response]` + `[presentation]`; preset `verifierFull` único implementado; `buildVerdictViewModel()` privado (exportado para tests).

**Alternativa descartada:** Headless + 5 átomos exportados — seam prematuro con un call site.

### D3 — CredentialAnchorsPanel compartido

**Decisión:** Panel de anclas usado por holder (modal) y por `VerificationVerdictPanel` (bloque credential). Explorer solo Sepolia (`11155111`).

### D4 — Holder: modal lazy + caché en componente

**Decisión:** Botón «Ver anclas» → modal; `Map<id, detail>` en `HolderComponent`; limpiar en logout.

**Alternativa descartada:** Caché en `HolderService` — un adapter = seam hipotético.

### D5 — Verifier query param sin auto-verify

**Decisión:** `ActivatedRoute` lee `credentialId`; pre-rellena input; usuario dispara verificación.

### D6 — RateLimitExceededError en misma iteración

**Decisión:** Documentar 429 en OpenAPI + mapear en `VerifierService` vía `toErrorCode`.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| Export OpenAPI requiere servicios arrancables | Documentar en tasks; CI `verify-openapi` valida snapshots |
| `VerdictPresentation` crece con presets futuros | Solo `verifierFull` implementado; presets adicionales cuando haya call site |
| Caché stale entre sesiones en mismo tab | Limpiar en `handleLogout()` |
| Verifier UI mezcla inglés/español | Fuera de alcance; no refactorizar copy en este cambio |

## Migration Plan

1. Backend OpenAPI transformers + export (sin cambio de runtime HTTP).
2. Regen clientes (Kiota BFF, ng-openapi-gen web).
3. Nuevos módulos UI + tests builder.
4. Refactor verifier + holder + query param.
5. Actualizar `CONTEXT.md` y `future-work.md`.

Rollback: revertir PR; snapshots anteriores restauran tipos (runtime no cambia).

## Open Questions

Ninguna pendiente — decisiones cerradas en sesión grill.
