## 1. Cliente API

- [x] 1.1 Regenerar cliente BFF Angular (`ng-openapi-gen`) tras snapshot del cambio backend.
- [x] 1.2 Añadir `anchorDocument(institutionId, document)` en `IssuerApiService` (fachada sobre cliente generado o HTTP directo al BFF).
- [x] 1.3 Definir tipo `ContentAnchor` en `credential.models.ts` si no viene del codegen.

## 2. Refactor emisión

- [x] 2.1 Actualizar `TitleIssuanceService.issueTitle`: anchor backend → on-chain → link title.
- [x] 2.2 Eliminar inyección/uso de `IpfsPinningService`.
- [x] 2.3 Asegurar que errores del anchor propagan a `CredentialService` / `IssuerTabComponent`.

## 3. Limpieza legacy

- [x] 3.1 Eliminar archivo `ipfs-pinning.service.ts`.
- [x] 3.2 Buscar y eliminar referencias a `sovereignid.pinata.jwt` y `IPFS_GATEWAY_BASE` si solo usados por pinning legacy.
- [x] 3.3 Actualizar imports en módulos/tests afectados.

## 4. UX y mensajes

- [x] 4.1 Mapear `ipfs_not_configured`, `content_anchor_failed`, `invalid_anchor_document` en `toErrorMessage` o handler del tab emisor.
- [x] 4.2 Verificar que el botón "Emitir" muestra estado de carga durante anchor + on-chain.

## 5. Documentación y verificación

- [x] 5.1 Actualizar `docs/deployment.md`: Pinata solo en issuer-api (variables `Issuer__ContentAnchor__*`).
- [x] 5.2 Actualizar `CONTEXT.md` flujo de emisión (sin pinning browser).
- [x] 5.3 `npm run build` en `src/web`.
- [ ] 5.4 Smoke manual: emisión completa con issuer dev + Pinata configurado.
