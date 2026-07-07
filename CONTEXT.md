# SovereignID — Contexto del monorepo

Este repositorio es el **monorepo SovereignID**: plataforma de identidad soberana con microservicios independientes desplegables en contenedor.

> **Desambiguación:** el nombre *SovereignID* también aparece en repositorios legados y documentación histórica. **Este repo** es el monorepo greenfield descrito aquí; no asumir paridad de rutas ni código con implementaciones anteriores bajo `src/bc-auth/` u otros árboles legados.

## Mapa del monorepo

```
SovereignID/
├── SovereignID.sln          # Solución raíz
├── CONTEXT.md               # Este archivo
├── database/                # Esquema SQL canónico (fuente de verdad)
│   └── BBDD_SovereignID.sql
├── docs/                    # Contratos y documentación transversal
│   ├── adr/                 # Architecture Decision Records
│   └── contracts/           # OpenAPI snapshots y fixtures JSON
├── scripts/
│   ├── scaffold-auth-db.ps1 # Regenerar modelo EF (database-first)
│   └── gen-kiota-clients.ps1 # Regenerar clientes Kiota del BFF
├── openspec/                # Cambios y specs OpenSpec
├── src/
│   ├── auth/                # Microservicio de autenticación SIWE
│   ├── bff/                 # Backend-for-Frontend (Kiota → microservicios)
│   │   ├── Bff.Api/         # Interfaz HTTP pública hacia el portal web
│   │   └── Bff.Clients/     # Clientes Kiota generados (Generated/)
│   └── …                    # verifier, issuer, academy, reports, web
└── tests/
    └── auth/
        └── Auth.IntegrationTests/  # AC-01…AC-07
```

Futuros microservicios seguirán `src/{servicio}/` (carpeta en minúscula) con capas análogas.

## Servicio `auth`

Autenticación **Sign-In with Ethereum (EIP-4361)** para Ethereum Sepolia:

| Endpoint | Propósito |
|----------|-----------|
| `GET /auth/nonce` | Emite reto (nonce 32 hex, TTL 600 s) |
| `POST /auth/verify` | Verifica mensaje SIWE firmado y emite JWT 24 h |

Contratos del servicio auth (dos capas complementarias):

| Capa | Fuente | Qué define |
|------|--------|------------|
| **Contrato HTTP (forma JSON)** | OpenAPI generado por `Auth.Api` | Rutas, DTOs de request/response, códigos HTTP |
| **Contrato de dominio (semántica)** | [`docs/siwe-backend-contract.md`](docs/siwe-backend-contract.md) | Reglas SIWE, chain policy, catálogo de errores, AC-01…AC-07 |

El OpenAPI es la **fuente de verdad** para nombres y tipos de campos JSON (`jwt`, `expiresAt`, `platformAdmin`, `holder`, `memberships`, …). El markdown complementa lo que el schema no expresa (p. ej. `nonce_consumed`, TTL del auth challenge).

**RBAC en JWT:** tras verify exitoso, `auth-api` enriquece el JWT (y la respuesta HTTP) con `user_id`, `platform_admin`, `holder` y claims repetibles `membership` (`{institutionId}:{role}`). La allowlist de platform admin vive en `Auth__PlatformAdminAddresses`. Los roles institucionales se resuelven desde Postgres (`users`, `institution_users`); el holder desde `student_wallets` primaria activa. **Enforcement downstream:** `academy-api` e `issuer-api` validan JWT + politicas; el BFF solo reenvia `Authorization` (ADR-0005). El portal Angular usa `roleGuard` en `/platform`, `/issuer` y `/holder`.

**Roles demo (seed-dev):**

| Rol | Wallet ejemplo | Acceso |
|-----|----------------|--------|
| Platform admin | wallets en `Auth__PlatformAdminAddresses` | Crear instituciones (`/platform`) |
| Issuer institucional | `0x1111…1111` (usuario seed Duoc UC) | Portal emisor |
| Holder | `0xf6461f392288b5732a7703e8b83f64cab134eada` | Portal titular |

**Frontend (web) — auth:** el cliente HTTP se **genera desde `docs/contracts/auth.openapi.json`** con `ng-openapi-gen` → `src/app/api/auth/` (`npm run gen:api:auth`, `rootUrl` vacío — rutas `/auth/*` directas a `auth-api`). `AuthApiService` envuelve el cliente generado y aplica `error.utils.ts`. El estado de sesión del cliente usa el mismo nombre que el wire: **`jwt`** (no `token`). Tras verify exitoso, la **`address` de sesión proviene de la respuesta HTTP** (identidad certificada por auth), no de la lectura previa de la wallet. El cliente **persiste `expiresAt`** del JWT y restaura sesión solo si aún no ha caducado. El mensaje SIWE usa **chain ID Sepolia (`11155111`)** vía constante; v1 no comprueba ni fuerza el cambio de red en la wallet antes de firmar.

**Frontend (web) — sistema de diseño:** el mapa UX/UI y la propuesta de sistema de diseño viven en [`docs/web-design-system.md`](docs/web-design-system.md). El roadmap Academy/roles vive en [`docs/web-academy-ui-todo.md`](docs/web-academy-ui-todo.md). La app usa Angular standalone + Tailwind; hoy los patrones visuales están embebidos en templates y deben formalizarse gradualmente en `shared/ui`.

**Frontend (web) — portales verifier/holder:** el cliente HTTP se **genera desde `docs/contracts/bff.openapi.json`** con `ng-openapi-gen` → `src/app/api/bff/` (`npm run gen:api:bff`, `rootUrl = '/api'`). Las fachadas `VerifierService` y `HolderService` envuelven el cliente BFF y aplican el seam de Problem Details (`error.utils.ts`). Ver [ADR-0004](docs/adr/0004-openapi-client-codegen.md) y [ADR-0005](docs/adr/0005-bff-kiota.md).

## Servicio `verifier`

Verificación pública (anónima) de **Verifiable Credentials** emitidas a estudiantes:

| Endpoint | Propósito |
|----------|-----------|
| `POST /verifications` | Verifica una VC por su UUID (`credentials.id`) y devuelve un veredicto (`result`) |
| `GET /health` | Liveness check |

Contratos del servicio verifier (dos capas, igual que auth):

| Capa | Fuente | Qué define |
|------|--------|------------|
| **Contrato HTTP (forma JSON)** | OpenAPI generado por `Verifier.Api` → `docs/contracts/verifier.openapi.json` | Rutas, DTOs, códigos HTTP |
| **Contrato de dominio (semántica)** | [`docs/verifier-backend-contract.md`](docs/verifier-backend-contract.md) | Reglas de veredicto, precedencia, errores, criterios VER-01… |

**Modelo de respuesta:** un **veredicto de negocio** (credencial revocada, expirada, inexistente o con integridad fallida) es un resultado legítimo y se devuelve como **`200 OK` con campo `result`** ∈ { `valid`, `revoked`, `expired`, `not_found`, `integrity_failed` }. Problem Details (RFC 7807) cubre errores de entrada/protocolo → `400` (`invalid_credential_id`) y `429` (`rate_limit_exceeded`). Ver [`docs/verifier-backend-contract.md`](docs/verifier-backend-contract.md).

**Veredicto escalonado:** se computan contra la BD los chequeos locales (`found`, `notRevoked`, `notExpired`). Los chequeos de evidencia (`hashMatches`, `onChainExists`, `signatureValid`) se activan por configuración (`Verifier:Evidence:*CheckEnabled`, todos `false` por defecto) y pueden devolver `null` cuando están deshabilitados o la infraestructura externa no responde. Precedencia: `not_found` > `revoked` > `expired` > `integrity_failed` > `valid`. Revocación on-chain puede elevar a `revoked` aunque BD diga activa; `revocationSource` y `validationSource` trazan la procedencia. Cada intento se registra en `verification_logs` con todos los chequeos y fuentes.

**Portal web del verifier:** la entrada en v1 es el **UUID** (`credentialId`) introducido en un campo de texto (el escáner QR queda para un cambio posterior; el QR codifica el UUID en crudo, no una URL navegable). El componente habla solo con una **fachada `VerifierService`** que envuelve el cliente HTTP generado (`ng-openapi-gen`) y aplica el seam de Problem Details (`error.utils.ts`). El veredicto se renderiza completo: badge de `result`, lista de `checks` (los `null` como "no evaluado") y bloque `credential` (emisor, fechas, anclas). El front llega al backend vía **`/api/verifications`** (nginx strip → `bff-api` → Kiota → `verifier-api`). El campo `result` del contrato se modela como `enum` para que el cliente generado lo tipe como unión.

## Modelo de errores HTTP (transversal)

Todos los microservicios del monorepo responden errores de negocio con **RFC 7807 Problem Details** (`application/problem+json`). Referencia de implementación: `AuthFailureExceptionFilter` en auth.

| Campo | Obligatorio | Uso |
|-------|-------------|-----|
| `title` | sí | Resumen corto del tipo de error (p. ej. `"Authentication failed"`) |
| `status` | sí | Código HTTP (`400`, `401`, …) |
| `detail` | sí | Mensaje legible para humanos — **fuente para mostrar al usuario en web** |
| `error` | sí | Código estable en `snake_case` para lógica programática (p. ej. `nonce_expired`) |

**Web:** un único seam en `error.utils.ts` parsea Problem Details y expone `detail` al usuario. Los componentes no leen el cuerpo HTTP directamente.

**Backend:** cada servicio mapea fallos de dominio a Problem Details con extensión `error`. Los catálogos de códigos viven en el contrato de dominio de cada servicio. Decisión registrada en [ADR-0001](docs/adr/0001-problem-details-errors.md).

## Documentación de APIs (OpenAPI)

Todos los microservicios documentan su contrato HTTP con el estándar **OpenAPI 3.1** usando el generador nativo de .NET 10 (`Microsoft.AspNetCore.OpenApi`). Los comentarios XML (`<summary>`, `<remarks>`) de controladores enriquecen automáticamente el documento (`<GenerateDocumentationFile>` activado en cada `*.Api`).

| Recurso | Ruta | Disponibilidad |
|---------|------|----------------|
| Documento OpenAPI (JSON) | `GET /openapi/v1.json` | Solo `Development` |
| UI navegable (Scalar) | `GET /scalar/v1` | Solo `Development` |

Los metadatos del documento (título, descripción, contacto) se definen por servicio en `*.Api/OpenApi/{Servicio}OpenApiExtensions.cs` (`Add{Servicio}OpenApiDocumentation` / `Map{Servicio}OpenApiDocumentation`). El scaffold (`scripts/scaffold-microservice.ps1`) genera este patrón para nuevos servicios.

**Snapshots versionados** — la fuente de verdad del contrato HTTP vive en `docs/contracts/{servicio}.openapi.json` (sin bloque `servers`, host-independiente):

| Acción | Comando |
|--------|---------|
| Exportar/regenerar snapshots | `bash scripts/export-openapi.sh [servicio\|all]` |
| Verificar snapshots vs. servicio vivo | `bash scripts/verify-openapi.sh [servicio\|all]` |
| Regenerar clientes Kiota del BFF | `.\scripts\gen-kiota-clients.ps1` |

El registro de servicios (proyecto, puerto de export, ruta de snapshot) está en `scripts/openapi-lib.sh`. CI ejecuta el job **`verify-openapi`** que falla si algún snapshot está desactualizado; tras cambiar un contrato hay que reexportar y commitear el JSON.

## Base de datos y persistencia

PostgreSQL 16 en Docker (`docker-compose.yml`, servicio `postgres`). El esquema se aplica al primer arranque desde `database/BBDD_SovereignID.sql`.

| Aspecto | Regla |
|---------|--------|
| Fuente de verdad | `database/BBDD_SovereignID.sql` — no EF Code-First |
| Modelo .NET | Database-first con EF Core Power Tools (`scripts/scaffold-auth-db.ps1`) |
| Consumo en código | Interfaces en Application → adapters en Infrastructure |
| DbContext | `internal` en Infrastructure; no inyectar en Api ni casos de uso |
| Proveedor auth | `Persistence:Provider` = `InMemory` (default) \| `Postgres` |

Decisión completa: [ADR-0002](docs/adr/0002-database-consumption.md).

Layout en `Auth.Infrastructure/Persistence/`:

| Zona | Carpeta | Contenido |
|------|---------|-----------|
| Generada (efcpt) | `Generated/` | `SovereignIdDbContext`, entidades EF en `Generated/Entities/` |
| Adapters | `Stores/<Aggregate>/` | Implementaciones de interfaces Application (p. ej. `ChallengeStore/`) |
| Composición DI | `Composition/` | `AddAuthPersistence()`, opciones, registro Npgsql |

**Stack local:**

```bash
docker compose up -d postgres          # levantar solo BD
.\scripts\scaffold-auth-db.ps1       # regenerar modelo EF (requiere postgres healthy)
```

Connection string desde contenedor: `Host=postgres;Port=5432;Database=sovereignid;Username=sovereignid;Password=sovereignid_dev`. Ver `.env.example`.

**Seed de desarrollo (Postgres):** tras `docker compose up -d postgres`, ejecutar `.\scripts\seed-dev.ps1`. Pobla catálogo `credential_types`, institución demo Duoc UC, un estudiante titular con wallet primaria, y credenciales de prueba. Re-ejecutable (upsert por UUID/código). Requiere `issuer-api` y `verifier-api` con `Persistence:Provider=Postgres` para probar `/holder` y `/verifier` contra datos reales.

## Glosario

| Término | Significado |
|---------|-------------|
| **SIWE** | Sign-In with Ethereum (EIP-4361): mensaje estructurado firmado con `personal_sign` |
| **Auth challenge** | Reto de un solo uso: nonce + emisión + caducidad + estado consumido |
| **Nonce** | 32 caracteres hex minúsculas (128 bits), emitido por `GET /auth/nonce` |
| **OriginalPayload** | Mensaje SIWE exactamente como fue firmado; base de verificación EIP-191 |
| **personal_sign** | Firma off-chain con prefijo EIP-191 sobre UTF-8 del mensaje |
| **JWT de sesión** | Token HS256 post-login con claims `sub`, `address`, `did`, etc. |
| **Sepolia** | Red de prueba Ethereum; chain ID **11155111** (única aceptada en v1) |
| **Contrato HTTP** | OpenAPI del servicio auth: forma observable de endpoints y DTOs JSON |
| **Contrato de dominio** | Documento markdown con reglas de negocio, errores estables y criterios de aceptación |
| **Problem Details** | Formato RFC 7807 de respuesta de error HTTP usado transversalmente en el monorepo |
| **Código de error** | Valor estable en extensión `error` (`snake_case`); distinto del texto `detail` mostrado al usuario |
| **Adapter de persistencia** | Implementación en Infrastructure de una interfaz de Application; único lugar con EF y mapeo dominio ↔ BD |
| **Entidad EF** | Clase scaffold en `Persistence/Generated/Entities` (namespace `Generated.Entities`); representa fila de tabla, no modelo de dominio |
| **Proveedor de persistencia** | `InMemory` o `Postgres`; selecciona el adapter activo sin cambiar casos de uso |
| **Titular (holder)** | Estudiante con wallet primaria activa en `student_wallets`. SIWE no crea fila en BD; issuer filtra credenciales por claim `did` del JWT contra `credentials.subject_did` (derivado de la wallet en minúsculas). Distinto de `users` (usuarios institucionales). |
| **Seed de desarrollo** | Datos ficticios idempotentes en `database/seed-dev.sql`, aplicados con `scripts/seed-dev.ps1` contra Postgres local Docker. Refresca fixtures demo a estado canónico; no forma parte del esquema canónico. |
| **Reporte institution-scoped** | KPI de una sola institución; path `/reports/institutions/{institutionId}/…`; consumidor UI: tab Reportes en `/academy`. |
| **Reporte platform-scoped** | KPI cross-tenant; path `/reports/platform/…`; consumidor UI: sección reportes en `/platform`. |
| **Verificaciones (R-I2)** | Intentos de verificación registrados en `verification_logs` para credenciales de la institución; en UI no usar el término "leídas". |
| **Fuente del reporte (`source`)** | Metadato del backend: `snapshot` (día pre-agregado), `live` (query en tiempo real), `hybrid` (mezcla). |
| **Período de reporte** | Rango `{from, to}` compartido por todos los reportes de un portal; presets 7/30/90 días (default 30). Reportes puntuales usan `asOf = to`. |
| **Reparto identitario v1** | Sin microservicio `identity` en MVP. Identidad criptográfica en `auth`; datos académicos/tenant y perfil holder en `academy`; credenciales en `issuer`. Ver [ADR-0006](docs/adr/0006-consolidate-identity-into-academy-auth.md). |
| **Usuario (`users`)** | Fila por wallet/DID. Usuarios institucionales (invitaciones) y, lazy, holders que editan perfil off-chain. Distinto de `students` (registro anónimo por institución). |
| **Wallet dual-role** | Una misma address puede ser holder (`student_wallets` primaria) y usuario institucional (`institution_users`) a la vez; JWT incluye ambos claims. |

## Reparto identitario v1

El documento inicial del proyecto contemplaba `Identity.API` para CRUD de instituciones y alumnos. En el monorepo greenfield ese alcance vive en **`academy`**; no existe contenedor `identity-api` en MVP ([ADR-0006](docs/adr/0006-consolidate-identity-into-academy-auth.md)).

| Concern | Módulo | Rutas / mecanismo |
|---------|--------|-------------------|
| SIWE + JWT + RBAC | `auth` | `/auth/*`; enriquece JWT con `user_id`, `platform_admin`, `holder`, `membership` |
| Instituciones, carreras, estudiantes, invitaciones, usuarios institucionales | `academy` | `/academy/*` |
| Perfil off-chain del titular | `academy` | `GET/PUT /academy/holders/me` |
| Credenciales del titular | `issuer` | `/issuer/holders/me/credentials` |
| Políticas JWT downstream | `SovereignID.Authorization` | Cada API consumidora |

**Huecos MVP pendientes** (alcance original de Identity, hoy en academy): update/delete de instituciones, list/get/update de carreras, update de estudiantes, auditoría administrativa (`audit_logs`).

## Configuración relevante

| Variable / clave | Uso |
|------------------|-----|
| `AUTH_JWT_SIGNING_KEY` | Clave simétrica JWT (≥ 32 bytes UTF-8 fuera de Development) |
| `Auth:JwtIssuer` / `Auth:JwtAudience` | Claims `iss` y `aud` del JWT |
| `Auth:ChallengeTtlSeconds` | TTL del reto (default 600) |
| `Auth:JwtTtlHours` | TTL del JWT (default 24) |
| `Persistence:Provider` | `InMemory` (default) o `Postgres` para auth challenges |
| `ConnectionStrings:DefaultConnection` | Cadena Npgsql; obligatoria si `Persistence:Provider=Postgres` |
| `POSTGRES_*` | Variables Docker del servicio `postgres` (ver `.env.example`) |

## Ejecución local

```bash
dotnet run --project src/auth/Auth.Api
dotnet test tests/auth/Auth.IntegrationTests
```

Puerto HTTP de desarrollo: `http://localhost:5132` (ver `Properties/launchSettings.json`).

## Servicio `academy`

El microservicio `academy` concentra el dominio academico del MVP: instituciones, carreras, estudiantes e invitaciones institucionales.

| Endpoint | Proposito |
|----------|-----------|
| `POST /academy/institutions` | Crea institucion y genera una invitacion admin |
| `GET /academy/institutions` | Lista instituciones para platform admin |
| `GET /academy/institutions/{institutionId}` | Consulta institucion |
| `POST /academy/institutions/{institutionId}/careers` | Crea carrera |
| `POST /academy/institutions/{institutionId}/students` | Crea estudiante, con wallet opcional |
| `GET /academy/institutions/{institutionId}/students` | Lista estudiantes de la institucion |
| `GET /academy/institutions/{institutionId}/students/{studentId}` | Consulta estudiante |
| `POST /academy/institutions/{institutionId}/students/{studentId}/wallets` | Vincula wallet manual a estudiante |
| `POST /academy/institutions/{institutionId}/invitations` | Invita un usuario institucional |
| `POST /academy/institutions/{institutionId}/users/invitations` | Alias para invitar usuario institucional |
| `GET /academy/institutions/{institutionId}/users` | Lista usuarios institucionales |
| `PATCH /academy/institutions/{institutionId}/users/{userId}/role` | Cambia rol institucional |
| `DELETE /academy/institutions/{institutionId}/users/{userId}` | Revoca acceso institucional |
| `POST /academy/invitations/accept` | Acepta invitacion y vincula wallet MetaMask existente |
Regla MVP: el backend **no crea cuentas MetaMask**. Las wallets son existentes y se vinculan cuando el usuario acepta una invitacion o cuando la institucion registra la wallet del estudiante. El link de invitacion expira; en BD se persiste solo el hash SHA-256 del token, no el token crudo.

Roles MVP: `platform_admin` administra tenants/instituciones desde `user_global_roles`; `admin`, `issuer` y `viewer` viven en `institution_users` acotados a una institucion. El primer platform admin se crea con `database/seed-platform-admin.sql`; sobre una BD existente aplicar antes `database/patches/2026-07-04-academy-authz.sql`.

La wallet/DID emisor de la institucion y la emision o vinculacion de titulos/credenciales quedan en el servicio `issuer`.

Contrato de dominio: [`docs/academy-domain-contract.md`](docs/academy-domain-contract.md). Contrato de autorizacion: [`docs/authorization-domain-contract.md`](docs/authorization-domain-contract.md).

## Servicio `issuer`

El microservicio `issuer` concentra la emision y vinculacion de credenciales verificables.

| Endpoint | Proposito |
|----------|-----------|
| `POST /issuer/institutions/{institutionId}/wallet` | Vincula wallet/DID emisor de una institucion |
| `POST /issuer/students/{studentId}/title` | Vincula un titulo emitido a un estudiante |
| `GET /issuer/holders/me/credentials` | Lista credenciales del titular autenticado (JWT SIWE) |
| `GET /issuer/holders/me/credentials/{credentialId}` | Detalle de credencial del titular |
| `GET /issuer/credentials/{credentialId}` | Detalle autenticado (ownership por `subject_did`) |

Regla MVP: `issuer` vincula la wallet/DID emisor de la institucion y, para vincular un titulo, el estudiante debe tener wallet primaria activa y la institucion debe tener DID emisor. El servicio valida carrera, tipo de credencial, datos IPFS, hash, transaccion y firma EIP-712 antes de registrar la fila en `credentials`. Las consultas del portal Holder filtran por claim `did` del JWT contra `credentials.subject_did`.

Contrato HTTP: OpenAPI generado por `Issuer.Api` → `docs/contracts/issuer.openapi.json` (incluye `bearerAuth` en operaciones holder y `status` como `enum` tipado). Contrato de dominio: [`docs/issuer-domain-contract.md`](docs/issuer-domain-contract.md).

**Portal web del holder:** `/holder` requiere sesión SIWE (`authGuard`) y carga credenciales reales con `GET /issuer/holders/me/credentials`. El componente habla solo con **`HolderService`**, fachada sobre el cliente generado (`ng-openapi-gen` → `src/app/api/issuer/`). El JWT se adjunta vía interceptor global y la fachada falla temprano si no hay sesión. Estados UI: `loading` / `loaded` / `empty` / `error`; badge de `status` (`active|revoked|expired`); icono por `typeCode` (`TITULO` → degree). Download JSON usa el detalle holder; Share QR v1 copia el UUID al portapapeles. El front llega al backend vía **`/api/issuer/…`** (nginx strip → `bff-api` → Kiota → `issuer-api`; JWT reenviado sin validar en BFF v1).

## Servicio `reports`

Microservicio de **solo lectura** para dashboards institution y platform (KPIs operativos). Backend v1 completo; la UI Angular consume los endpoints vía BFF (`/api/reports/…`).

| Endpoint | Perfil | Propósito |
|----------|--------|-----------|
| `GET /reports/institutions/{id}/credentials-issued` | `admin` / `issuer` / `viewer` (+ platform admin) | Emisiones diarias (snapshot + live tail) |
| `GET /reports/institutions/{id}/credential-reads` | idem | Verificaciones (leídas) por día |
| `GET /reports/institutions/{id}/credentials-per-student` | idem | Promedio credenciales / alumno activo (`asOf`) |
| `GET /reports/institutions/{id}/credentials-revoked` | idem | Revocaciones en período |
| `GET /reports/institutions/{id}/verification-outcomes` | idem | Válidas vs inválidas |
| `GET /reports/platform/credentials-by-institution` | `platform_admin` | Ranking emisiones cross-tenant |
| `GET /reports/platform/students-by-institution` | `platform_admin` | Stock alumnos activos por institución |

Autorización: librería `SovereignID.Authorization` (`PlatformOrInstitutionMember` en rutas `{institutionId}`, `PlatformAdmin` en `/reports/platform/*`). Persistencia database-first read-only sobre `credentials`, `verification_logs`, `students`, `institutions`, `institution_metrics_daily`.

**Job de snapshot:** `MetricsSnapshotJob` (nightly UTC) + CLI `dotnet run --project src/reports/Reports.Api -- snapshot --from YYYY-MM-DD --to YYYY-MM-DD` para backfill tras `seed-dev.ps1` cuando `Persistence:Provider=Postgres`.

Contrato HTTP: `docs/contracts/reports.openapi.json`. BFF pass-through: `/api/reports/…` → `reports-api` (JWT reenviado).

**Frontend (web) — reportes:** tab **Reportes** en portal `/academy` (reportes institution-scoped R-I1…R-I5) y tab **Reportes** en portal `/platform` (R-P1, R-P2), junto a tab **Instituciones** en platform. Alcance v1 de componentización: **`platform-institutions-tab`**, **`platform-reports-tab`** y **`academy-reports-tab`** como hijos standalone; tabs Estudiantes/Usuarios de Academy permanecen en el shell del portal. Tab Reportes en Academy visible para roles **`admin`**, **`issuer`** y **`viewer`** (misma regla que Estudiantes; requiere institución seleccionada). Componentes de visualización compartidos en `shared/ui/reports/`. Cliente HTTP generado desde `docs/contracts/bff.openapi.json` (`npm run gen:api:bff`) + fachada `ReportsService` con seam de Problem Details. Series temporales (R-I1, R-I2, R-I4, R-I5) se renderizan con **AntV G2 v5** (`theme: classicDark`); R-I3 y rankings platform usan KPI cards + gráfico de barras horizontal G2. En UI, R-I2 se etiqueta **Verificaciones** (no "leídas"): son intentos en `verification_logs`, no descargas IPFS. Selector de período compartido por portal: presets **7 / 30 / 90 días** (default **30**); reportes con `asOf` (R-I3, R-P2) usan `asOf = to` del rango seleccionado. Carga en tab Reportes: **`Promise.allSettled`** paralelo; error aislado por tarjeta con reintento local (`toErrorMessage` para Problem Details).

## Servicio `bff`

Backend-for-Frontend entre el portal web y los microservicios internos. Decisión: [ADR-0005](docs/adr/0005-bff-kiota.md).

| Aspecto | Detalle |
|---------|---------|
| Proyecto | `src/bff/Bff.Api` + `src/bff/Bff.Clients` (Kiota generado) |
| Contrato público | `docs/contracts/bff.openapi.json` (pass-through v1) |
| Prefijo browser | `/api/` (nginx strip → `bff-api:8080`) |
| Auth SIWE | **Fuera del BFF** — `/auth/` directo a `auth-api` |
| Downstream v1 | verifier, issuer (holder + admin), academy, reports |
| JWT holder | Reenvío del header `Authorization`; validación en `issuer-api` |

Rutas issuer admin expuestas en v1: `POST /issuer/institutions/{id}/wallet`, `POST /issuer/students/{id}/title`, `GET /issuer/credentials/{id}`.

Clientes Kiota se regeneran con `scripts/gen-kiota-clients.ps1` desde los snapshots de cada microservicio; código commiteado en `Bff.Clients/Generated/`. Los clientes Angular se regeneran con `npm run gen:api:bff` (desde `docs/contracts/bff.openapi.json`) y `npm run gen:api:auth` (desde `docs/contracts/auth.openapi.json`).

## CI/CD y despliegue

| Rama | CI (build + test) | Deploy |
|------|-------------------|--------|
| `dev` | Sí (push y PR) | No |
| `master` | Sí (push y PR) | Sí → webhook Portainer en VPS |

Flujo: `feature/*` → PR → `dev` → PR → `master` → deploy automático.

Detalle completo (secretos, Portainer, branch protection, SonarCloud): [`docs/deployment.md`](docs/deployment.md).
