## Why

El backend (`add-verifier-evidence-checks`, PR #34) ya expone lectura on-chain real en `POST /verifications`: chequeos de evidencia (`onChainExists`, `hashMatches`, `signatureValid`), fuentes trazables (`validationSource`, `revocationSource`) y veredicto `integrity_failed`. El frontend tiene el cliente BFF regenerado y una implementación inline en `verifier.component.ts`, pero el contrato generado miente sobre nullability de fuentes, el BFF no documenta `429`, la presentación del veredicto no es reutilizable, y el titular no ve las anclas on-chain que el verifier ahora valida. Hay que profundizar los módulos UI compartidos, alinear el seam de contrato y cerrar el ciclo titular → verifier.

## What Changes

- Corregir generación OpenAPI en **BFF y verifier** (`validationSource`/`revocationSource` nullable), documentar respuesta **429** en `bff.openapi.json`, reexportar snapshots y regenerar clientes Kiota + `ng-openapi-gen`.
- Extraer **`VerificationVerdictPanel`** (`shared/ui/verification-verdict/`): preset `verifierFull`, checks agrupados (registro local / evidencia on-chain), banner cuando evidencia está deshabilitada, `revocationSource` condicional.
- Extraer **`CredentialAnchorsPanel`** (`shared/ui/credential-anchors/`): anclas completas con `copy-value`, link IPFS, block explorer **solo Sepolia**.
- Refactorizar **`verifier.component`**: orquestación + `VerificationVerdictPanel`; **`RateLimitExceededError`** en `VerifierService`.
- Portal **holder**: modal «Ver anclas» con fetch lazy + caché local; CTA a `/verifier?credentialId=`; limpiar caché en logout.
- **`VerifierComponent`**: pre-rellenar UUID desde query param sin auto-verificar.
- Tests unitarios de `buildVerdictViewModel()`; actualizar `CONTEXT.md` y `add-verifier-evidence-checks/future-work.md`.

## Capabilities

### New Capabilities

- `verification-verdict-ui`: módulo compartido de presentación del veredicto de verificación (`VerificationVerdictPanel`, builder de view model, preset `verifierFull`, agrupación de checks, banner evidencia deshabilitada).
- `credential-anchors-ui`: módulo compartido de presentación de anclas on-chain/IPFS (`CredentialAnchorsPanel`, explorer Sepolia, composición desde verifier y holder).
- `bff-verifier-contract-alignment`: alineación del contrato BFF con verifier (nullable en fuentes de checks, respuesta 429, regeneración de clientes).

### Modified Capabilities

- `verifier-web-portal`: el portal verifier delega presentación del veredicto al módulo compartido; manejo explícito de `rate_limit_exceeded`; soporte de query param `credentialId` para pre-relleno.
- `holder-web-portal`: el titular puede inspeccionar anclas on-chain en modal y navegar al verifier con UUID pre-cargado.

## Impact

- **Backend (BFF + verifier):** `BffOpenApiExtensions.cs`, `VerifierOpenApiExtensions.cs`, `VerificationsController.cs` (BFF); snapshots `docs/contracts/bff.openapi.json`, `docs/contracts/verifier.openapi.json`; clientes Kiota en `Bff.Clients/Generated/`.
- **Frontend (`src/web/`):** nuevos módulos en `shared/ui/`, refactor `verifier.component.ts`, `holder.component.ts`, `verifier.service.ts`; cliente regenerado en `app/api/bff/`.
- **Tests:** `verification-verdict-model.builder.spec.ts` (nuevo).
- **Docs:** `CONTEXT.md`, `openspec/changes/add-verifier-evidence-checks/future-work.md`.
- **Sin breaking HTTP:** cambio de contrato es corrección de nullability y documentación de 429 ya soportado en runtime.
