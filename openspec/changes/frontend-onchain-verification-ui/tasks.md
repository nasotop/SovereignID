## 1. Contrato OpenAPI (BFF + verifier)

- [x] 1.1 Corregir nullable en `validationSource`/`revocationSource` en `BffOpenApiExtensions.cs`
- [x] 1.2 Corregir nullable en `VerifierOpenApiExtensions.cs` (upstream)
- [x] 1.3 Añadir respuesta `429` en transformer/controlador BFF (`VerificationsController`, `[ProducesResponseType]`)
- [x] 1.4 Reexportar snapshots: `bash scripts/export-openapi.sh bff verifier`
- [x] 1.5 Verificar snapshots: `bash scripts/verify-openapi.sh bff verifier`
- [x] 1.6 Regenerar clientes Kiota: `scripts/gen-kiota-clients.ps1`
- [x] 1.7 Regenerar cliente Angular: `npm run gen:api:bff` en `src/web/`

## 2. CredentialAnchorsPanel

- [x] 2.1 Crear `shared/ui/credential-anchors/` con tipos y componente standalone
- [x] 2.2 Implementar campos con `copy-value`, link IPFS gateway, explorer Sepolia (`11155111`)
- [x] 2.3 Implementar CTA opcional `routerLink` a `/verifier?credentialId=`
- [x] 2.4 Exportar barrel `index.ts`

## 3. VerificationVerdictPanel

- [x] 3.1 Crear `shared/ui/verification-verdict/` (types, labels, presets con `verifierFull`)
- [x] 3.2 Implementar `buildVerdictViewModel()` (agrupación, banner evidencia, `revocationSource` condicional)
- [x] 3.3 Implementar `VerificationVerdictPanelComponent` componiendo `CredentialAnchorsPanel` y `status-badge`
- [x] 3.4 Añadir `verification-verdict-model.builder.spec.ts` (≥4 casos)
- [x] 3.5 Exportar barrel `index.ts`

## 4. VerifierService y portal verifier

- [x] 4.1 Añadir `RateLimitExceededError` y mapeo en `VerifierService` vía `toErrorCode`
- [x] 4.2 Refactorizar `verifier.component.ts`: delegar a `VerificationVerdictPanel`, eliminar labels/formatters inline
- [x] 4.3 Leer `credentialId` desde query param al montar (pre-relleno, sin auto-verify)

## 5. Portal holder

- [x] 5.1 Añadir botón «Ver anclas» y modal con estado loading/error
- [x] 5.2 Implementar caché `Map<id, HolderCredentialDetail>` y fetch lazy en apertura de modal
- [x] 5.3 Reutilizar caché en `handleDownload` cuando el detalle ya existe
- [x] 5.4 Limpiar caché y cerrar modal en `handleLogout`

## 6. Documentación

- [x] 6.1 Actualizar `CONTEXT.md` (verifier panel compartido, holder modal anclas)
- [x] 6.2 Actualizar `openspec/changes/add-verifier-evidence-checks/future-work.md` (UX verifier completada)

## 7. Verificación final

- [x] 7.1 Ejecutar tests unitarios web (`verification-verdict-model.builder.spec.ts`)
- [ ] 7.2 Smoke manual: verifier con checks agrupados + banner dev; holder modal anclas → link verifier; 429 con mensaje legible
