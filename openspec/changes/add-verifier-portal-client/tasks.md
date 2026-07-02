## 1. Enriquecer el contrato OpenAPI (backend)

- [x] 1.1 Anotar `result` como `enum` (`valid|revoked|expired|not_found`) en el documento OpenAPI de `Verifier.Api` (schema transformer en `VerifierOpenApiExtensions.cs` o enum con `JsonConverter` snake_case), sin alterar `ToWireValue` ni el valor del wire
- [x] 1.2 Reexportar el snapshot con `bash scripts/export-openapi.sh verifier` y commitear `docs/contracts/verifier.openapi.json`
- [x] 1.3 Verificar que `result` aparece como `enum` con los cuatro valores en el snapshot y que `verify-openapi` queda en verde

## 2. Configurar el codegen del cliente (web)

- [x] 2.1 Añadir `ng-openapi-gen` a `devDependencies` en `src/web/package.json`
- [x] 2.2 Crear `src/web/ng-openapi-gen.json` con `input` = `../../docs/contracts/verifier.openapi.json`, `output` = `src/app/api/verifier`, y `rootUrl` vacío
- [x] 2.3 Añadir el script `gen:api:verifier` a `package.json`
- [x] 2.4 Ejecutar la generación y commitear el cliente generado en `src/web/src/app/api/verifier/` (modelos + servicio)
- [x] 2.5 Confirmar que el modelo generado tipa `result` como la unión de los cuatro valores

## 3. Fachada de aplicación con seam de errores (web)

- [x] 3.1 Crear `core/services/verifier.service.ts` que inyecta el `VerificationsService` generado
- [x] 3.2 Validar el formato UUID del `credentialId` antes de invocar al backend
- [x] 3.3 Ejecutar la verificación y traducir errores con `error.utils.ts` (`toThrownError`/`toErrorCode`), sin exponer `HttpErrorResponse` al componente
- [x] 3.4 Exponer un resultado tipado (veredicto + checks + credential) al componente

## 4. Reescribir el componente del portal (web)

- [x] 4.1 Sustituir el drag & drop por un campo de texto de `credentialId` (UUID) y un botón de verificación
- [x] 4.2 Gestionar estados con signals: `idle` / `loading` / `result` / `error` (patrón del login)
- [x] 4.3 Bloquear el envío y mostrar mensaje de validación cuando el UUID es vacío o malformado
- [x] 4.4 Renderizar el veredicto: badge de `result` distinguible por estado, lista de `checks` (los `null` como "no evaluado") y bloque `credential` (tipo, estado, emisor, fechas, anclas) cuando exista
- [x] 4.5 Renderizar el estado de error con el `detail` del Problem Details y opción de reintento

## 5. Transporte de punta a punta (infra)

- [x] 5.1 Añadir `location /verifications` en `src/web/nginx.conf` con `proxy_pass http://verifier-api:8080/verifications`
- [x] 5.2 Añadir el servicio `verifier-api` a `docker-compose.yml` (Dockerfile existente) en `sovereign-net`, con `ConnectionStrings__DefaultConnection`, `expose 8080` y `depends_on: postgres (healthy)`
- [x] 5.3 Ajustar `docker-compose.override.yml` si aplica (entorno Development del verifier) y `.env.example`
- [x] 5.4 Declarar `web depends_on: verifier-api` para el arranque ordenado

## 6. Verificación de extremo a extremo

- [ ] 6.1 Levantar el stack (`docker compose up` web + verifier-api + postgres) con datos de prueba en `credentials`
- [ ] 6.2 Validar los escenarios del spec `verifier-web-portal`: UUID válido (veredicto renderizado), UUID inválido (bloqueado en cliente), `not_found`, y `400 invalid_credential_id` (mensaje del seam)
- [x] 6.3 Ejecutar `npm run build` del front y confirmar que no hay errores de tipos con el cliente generado
