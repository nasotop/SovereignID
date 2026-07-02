## 1. Enriquecer contrato OpenAPI (backend issuer)

- [x] 1.1 Añadir `components.securitySchemes.bearerAuth` (HTTP Bearer JWT) en `IssuerOpenApiExtensions.cs`
- [x] 1.2 Aplicar `security: [{ bearerAuth: [] }]` a las operaciones GET holder (`/issuer/holders/me/credentials`, detalle, `/issuer/credentials/{id}`) vía document transformer o atributos OpenAPI
- [x] 1.3 Modelar `status` como `enum` (`active|revoked|expired`) en schemas `HolderCredentialSummary` y `HolderCredentialDetail` del documento OpenAPI
- [x] 1.4 Reexportar snapshot: `bash scripts/export-openapi.sh issuer` y commitear `docs/contracts/issuer.openapi.json` (sin bloque `servers`)
- [x] 1.5 Verificar que el snapshot contiene `bearerAuth`, `security` en operaciones holder, y `status` como enum; confirmar `verify-openapi` en verde

## 2. Configurar codegen del cliente issuer (web)

- [x] 2.1 Crear `src/web/ng-openapi-gen.issuer.json` con `input` = `../../docs/contracts/issuer.openapi.json`, `output` = `src/app/api/issuer`
- [x] 2.2 Añadir script `gen:api:issuer` en `src/web/package.json`
- [x] 2.3 Ejecutar generación y commitear cliente en `src/web/src/app/api/issuer/` (modelos + funciones + `Api`)
- [x] 2.4 Confirmar que el tipo generado de `status` es unión tipada (`active|revoked|expired`)

## 3. Fachada HolderService con JWT y seam de errores (web)

- [x] 3.1 Crear `core/services/holder.service.ts` que inyecta el `Api` generado y `AuthService`
- [x] 3.2 Adjuntar `Authorization: Bearer {jwt}` en llamadas holder; fallar temprano si no hay sesión válida
- [x] 3.3 Implementar `listMyCredentials()` y `getMyCredential(id)` traduciendo errores con `error.utils.ts`
- [x] 3.4 Implementar utilidad `downloadCredentialJson(detail)` para descarga de archivo `.json`
- [x] 3.5 Implementar `shareCredentialId(id)` (copiar UUID al portapapeles o equivalente)

## 4. Reescribir HolderComponent (web)

- [x] 4.1 Eliminar dependencia de `MOCK_HOLDER_CREDENTIALS`; cargar credenciales en `ngOnInit` vía `HolderService`
- [x] 4.2 Gestionar estados con signals: `loading` / `loaded` / `empty` / `error`
- [x] 4.3 Renderizar tarjetas desde API: título, emisor, fecha, badge `status`, icono por `typeCode`
- [x] 4.4 Conectar botón "Download JSON" al detalle holder
- [x] 4.5 Conectar botón "Share QR" a compartir UUID
- [x] 4.6 Manejar `401` (mensaje + opción login) y errores con `detail` + reintento

## 5. Transporte de punta a punta (infra)

- [x] 5.1 Añadir `location /issuer/` en `src/web/nginx.conf` → `proxy_pass http://issuer-api:8080/issuer/`
- [x] 5.2 Añadir servicio `issuer-api` en `docker-compose.yml` (Dockerfile existente), `sovereign-net`, Postgres, `depends_on: postgres (healthy)`
- [x] 5.3 Configurar `Auth__JwtSigningKey` (y demás vars) en compose para issuer-api alineadas con auth-api
- [x] 5.4 Declarar `web depends_on: issuer-api`; actualizar `.env.example` si aplica

## 6. Documentación transversal

- [x] 6.1 Actualizar `CONTEXT.md` — sección issuer + portal holder (cliente generado, proxy, fachada)
- [x] 6.2 Actualizar `docs/issuer-domain-contract.md` si el contrato OpenAPI añade detalles de seguridad no documentados

## 7. Verificación de extremo a extremo

- [x] 7.1 Ejecutar `dotnet test tests/issuer/Issuer.IntegrationTests` (backend holder)
- [x] 7.2 Ejecutar `npm run build` del front con cliente issuer generado
- [x] 7.3 Validar escenarios del spec `holder-web-portal`: carga con JWT, lista vacía, badge revoked, download JSON, 401 sin sesión
- [x] 7.4 (Opcional) Levantar stack docker y probar login SIWE → `/holder` con credenciales de prueba en Postgres
