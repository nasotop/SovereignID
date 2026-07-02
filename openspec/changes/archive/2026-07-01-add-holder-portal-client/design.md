## Context

El backend `issuer` ya implementa consultas holder (`GET /issuer/holders/me/credentials`, detalle por id, `GET /issuer/credentials/{id}`) con JWT Bearer validado localmente (`Auth:JwtSigningKey`, claims `did`/`address`). El snapshot `docs/contracts/issuer.openapi.json` existe pero **no declara** `components.securitySchemes` ni `security` en las operaciones GET — el codegen y consumidores externos no pueden inferir autenticación. El portal `holder` usa mocks estáticos pese a estar protegido por `authGuard` y tener sesión JWT en `AuthService`.

Restricciones vigentes:
- Seam único de Problem Details en `error.utils.ts` ([ADR-0001](../../../docs/adr/0001-problem-details-errors.md)).
- Cliente HTTP generado desde snapshot OpenAPI ([ADR-0004](../../../docs/adr/0004-openapi-client-codegen.md)).
- Rutas relativas proxeadas por nginx (patrón auth/verifier).
- Angular 22 standalone + signals; holder ya exige login SIWE.

## Goals / Non-Goals

**Goals:**
- Que `/holder` liste credenciales reales del titular autenticado.
- Documentar autenticación Bearer en OpenAPI y generar cliente tipado.
- Mantener el seam de errores; el componente no toca `HttpErrorResponse`.
- Dejar issuer alcanzable en el stack docker (gateway + `issuer-api`).

**Non-Goals:**
- Portal emisor (`/issuer`) — cambio posterior (`list + revoke`).
- Escáner QR en holder (v1: botón copia/muestra UUID; QR camera queda diferido).
- Migrar `auth` al codegen.
- Revocación de credenciales.
- Descarga del JSON-LD desde IPFS (v1: download de metadatos + anclas expuestas por la API).

## Decisions

### D1: Cliente generado con `ng-openapi-gen` desde snapshot issuer

Input: `docs/contracts/issuer.openapi.json`. Output: `src/web/src/app/api/issuer/`, `rootUrl: ''`. Script `npm run gen:api:issuer`. Código generado commiteado — mismo patrón que verifier.

**Alternativa:** extender `CredentialService` manual — reintroduce drift; el mock actual es síntoma del problema.

### D2: Fachada `HolderService` con JWT desde `AuthService`

El cliente generado no conoce la sesión SIWE. La fachada inyecta `AuthService`, obtiene `jwt` del estado de sesión, y pasa `Authorization: Bearer` en cada llamada holder. Si no hay JWT válido, la fachada falla antes de HTTP (coherente con `authGuard`, pero defensa en profundidad).

**Alternativa:** interceptor HTTP global de auth — cambio transversal; se reserva para cuando auth también migre a codegen.

### D3: Enriquecer OpenAPI con Bearer + enum `status`

`IssuerOpenApiExtensions` añade:
- `components.securitySchemes.bearerAuth` (HTTP bearer, JWT).
- `security: [{ bearerAuth: [] }]` en operaciones holder (`HolderCredentialsController`, `CredentialsController` GET).
- Schema transformer: `HolderCredentialSummary.status` y `HolderCredentialDetail.status` como `enum` [`active`, `revoked`, `expired`].

Sin alterar serialización JSON del backend (ya emite snake_case vía propiedades).

**Alternativa:** type union manual en front — contradice ADR-0004.

### D4: Mapeo UI: `typeCode` → icono, `status` → badge

| Campo API | UI |
|-----------|-----|
| `title` | título tarjeta |
| `issuerName` | "Issued by …" |
| `issuedAt` | fecha formateada (ISO → locale) |
| `typeCode === 'TITULO'` | icono `degree`; resto → `certificate` |
| `status` | badge active/revoked/expired con colores distintos |

### D5: Download JSON = detalle holder (`HolderCredentialDetail`)

Al pulsar "Download JSON", la fachada llama `GET /issuer/holders/me/credentials/{id}` (o reutiliza cache) y descarga un `.json` con el payload API (incluye `anchors`, `metadata`). No fetch IPFS en v1.

Share QR v1: copiar UUID (`id`) al portapapeles o mostrarlo — sin librería QR camera.

### D6: Transporte vía nginx + `issuer-api` en compose

`location /issuer/ { proxy_pass http://issuer-api:8080/issuer/; }` — rutas relativas `/issuer/holders/me/credentials`. Servicio `issuer-api` en `sovereign-net` con Postgres, `depends_on: postgres healthy`, `web depends_on: issuer-api`.

## Risks / Trade-offs

- [OpenAPI sin security hasta ahora] → Enriquecer antes de codegen; reexportar snapshot; CI `verify-openapi`.
- [Dos servicios con clientes generados (verifier + issuer)] → Scripts separados `gen:api:verifier` / `gen:api:issuer`; conviven con auth manual.
- [JWT expirado en holder] → Mostrar error del seam + redirect a login en 401; `AuthService` ya persiste `expiresAt`.
- [Lista vacía vs error] → Diferenciar `200 []` (estado empty) de error HTTP.
- [issuer-api requiere Postgres en compose] → Coherente con ADR-0002; documentar en `.env.example`.

## Migration Plan

1. Enriquecer OpenAPI Issuer (Bearer + enum) y reexportar snapshot.
2. Configurar `ng-openapi-gen` para issuer; generar y commitear cliente.
3. Implementar `HolderService` y reescribir `HolderComponent`.
4. Añadir nginx proxy + `issuer-api` en compose.
5. Verificación e2e: login SIWE → `/holder` → tarjetas desde API.

Rollback: revertir commit; holder vuelve a mocks (backend issuer sigue operativo).

## Open Questions

- Ninguna bloqueante. (Futuro: escáner QR, descarga JSON-LD desde IPFS gateway, portal emisor.)
