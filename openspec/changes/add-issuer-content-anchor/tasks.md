## 1. Dependencias y configuración



- [x] 1.1 Añadir paquete NuGet `Corvus.Text.Json` a `Issuer.Application` o `Issuer.Infrastructure` según convención del proyecto.

- [x] 1.2 Crear `ContentAnchorOptions` en `Issuer.Application` (`Enabled`, `VerifyEnabled`, `GatewayBase`, `PinataApiKey`, `PinataApiSecret`, `IpfsTimeoutSeconds`).

- [x] 1.3 Registrar opciones en `Issuer.Api` (`Issuer:ContentAnchor`); defaults `false`/`false` en `appsettings.json`.

- [x] 1.4 Documentar variables de entorno en `.env.example` y activar `true`/`true` en `appsettings.Development.json` / `docker-compose`.



## 2. Módulo `CredentialContentAnchor`



- [x] 2.1 Definir `IContentAnchorService` / `ContentAnchorResult` en `Issuer.Application`.

- [x] 2.2 Implementar `CanonicalJsonSerializer` wrapper sobre `JsonCanonicalizer` (JCS) + `ContentHashComputer` (SHA-256 hex).

- [x] 2.3 Definir seam `IContentPinningAdapter` con `PinAsync(ReadOnlyMemory<byte>, CancellationToken) → PinResult`.

- [x] 2.4 Implementar `PinataContentPinningAdapter` (`pinFileToIPFS`, API Key + Secret).

- [x] 2.5 Implementar `InMemoryContentPinningAdapter` para tests (almacena bytes, CID determinístico).

- [x] 2.6 Implementar `CredentialContentAnchorService` orquestador.

- [x] 2.7 Tests unitarios del serializador JCS con vectores RFC 8785 (mínimo 2 casos).



## 3. Verificación en link (`IContentAnchorVerifier`)



- [x] 3.1 Definir `IContentAnchorVerifier` y `ContentAnchorCheck` en `Issuer.Application`.

- [x] 3.2 Implementar `HttpContentAnchorVerifier` (GET gateway, SHA-256 bytes, timeout configurable).

- [x] 3.3 Implementar `NullContentAnchorVerifier`.

- [x] 3.4 Integrar en `IssuerService.LinkStudentTitleAsync` cuando `VerifyEnabled=true`.

- [x] 3.5 Añadir `IssuerFailureException` mapping para `content_anchor_invalid` (409).

- [x] 3.6 Tests unitarios/integración del verifier con `InMemoryContentPinningAdapter` + gateway simulado.



## 4. API issuer



- [x] 4.1 Crear `InstitutionDocumentsController` con `POST {institutionId}/documents/anchor`.

- [x] 4.2 Añadir DTOs `AnchorCredentialDocumentRequest` / `ContentAnchorResponse`.

- [x] 4.3 Aplicar política `InstitutionIssuer` y validación de membership.

- [x] 4.4 Mapear errores: `ipfs_not_configured` (503), `content_anchor_failed` (502), `invalid_anchor_document` (400).

- [x] 4.5 Tests integración `IssuerApiTests`: anchor exitoso (in-memory), sin config (503), forbidden (403).



## 5. BFF y contratos



- [x] 5.1 Añadir pass-through en `Bff.Api` hacia issuer anchor endpoint.

- [x] 5.2 Regenerar cliente Kiota (`scripts/gen-kiota-clients.ps1`).

- [x] 5.3 Reexportar `docs/contracts/issuer.openapi.json` y `docs/contracts/bff.openapi.json`.

- [x] 5.4 Verificar CI `verify-openapi`.



## 6. Datos y documentación



- [x] 6.1 Actualizar `database/seed-dev.sql` con credenciales cuyo `content_hash` sea coherente con JCS (o re-pinnear vía script documentado).

- [x] 6.2 Actualizar `docs/issuer-domain-contract.md` (nuevo endpoint, JCS, errores).

- [x] 6.3 Actualizar `docs/deployment.md` (Pinata API Key/Secret, GatewayBase).

- [x] 6.4 Actualizar `CONTEXT.md` sección issuer con flujo anchor backend.



## 7. Verificación final



- [x] 7.1 Ejecutar tests `Issuer.IntegrationTests`.

- [x] 7.2 Smoke manual: anchor con Pinata en dev → bytes recuperables por gateway → verify link con `VerifyEnabled=true`.


