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
│   └── …                    # verifier, issuer, academy, identity, web
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

El OpenAPI es la **fuente de verdad** para nombres y tipos de campos JSON (`jwt`, `expiresAt`, …). El markdown complementa lo que el schema no expresa (p. ej. `nonce_consumed`, TTL del auth challenge).

**Frontend (web) — auth:** el cliente HTTP se **genera desde `docs/contracts/auth.openapi.json`** con `ng-openapi-gen` → `src/app/api/auth/` (`npm run gen:api:auth`, `rootUrl` vacío — rutas `/auth/*` directas a `auth-api`). `AuthApiService` envuelve el cliente generado y aplica `error.utils.ts`. El estado de sesión del cliente usa el mismo nombre que el wire: **`jwt`** (no `token`). Tras verify exitoso, la **`address` de sesión proviene de la respuesta HTTP** (identidad certificada por auth), no de la lectura previa de la wallet. El cliente **persiste `expiresAt`** del JWT y restaura sesión solo si aún no ha caducado. El mensaje SIWE usa **chain ID Sepolia (`11155111`)** vía constante; v1 no comprueba ni fuerza el cambio de red en la wallet antes de firmar.

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

**Modelo de respuesta:** un **veredicto de negocio** (credencial revocada, expirada o inexistente) es un resultado legítimo y se devuelve como **`200 OK` con campo `result`** ∈ { `valid`, `revoked`, `expired`, `not_found` }. Problem Details (RFC 7807) se reserva **solo** para errores de entrada/protocolo → `400` con `error = invalid_credential_id`. Esto matiza la sección transversal «Modelo de errores HTTP» para el verifier (ver `docs/verifier-backend-contract.md`).

**Veredicto escalonado v1:** se computan contra la BD los chequeos sin red (`found`, `notRevoked`, `notExpired`); los chequeos con dependencia externa (`hashMatches`, `onChainExists`, `signatureValid`) se devuelven `null` (reservados). La expiración se computa desde `expires_at` vía `TimeProvider` (no se confía solo en `status`). Precedencia: `not_found` > `revoked` > `expired` > `valid`. Cada intento se registra en `verification_logs` (incluido `not_found`, con `credential_id = NULL`).

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
| `GET /academy/institutions/{institutionId}` | Consulta institucion |
| `POST /academy/institutions/{institutionId}/careers` | Crea carrera |
| `POST /academy/institutions/{institutionId}/students` | Crea estudiante, con wallet opcional |
| `POST /academy/institutions/{institutionId}/invitations` | Invita un usuario institucional |
| `POST /academy/invitations/accept` | Acepta invitacion y vincula wallet MetaMask existente |
Regla MVP: el backend **no crea cuentas MetaMask**. Las wallets son existentes y se vinculan cuando el usuario acepta una invitacion o cuando la institucion registra la wallet del estudiante. El link de invitacion expira; en BD se persiste solo el hash SHA-256 del token, no el token crudo.

La wallet/DID emisor de la institucion y la emision o vinculacion de titulos/credenciales quedan en el servicio `issuer`.

Contrato de dominio: [`docs/academy-domain-contract.md`](docs/academy-domain-contract.md).

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

## Servicio `bff`

Backend-for-Frontend entre el portal web y los microservicios internos. Decisión: [ADR-0005](docs/adr/0005-bff-kiota.md).

| Aspecto | Detalle |
|---------|---------|
| Proyecto | `src/bff/Bff.Api` + `src/bff/Bff.Clients` (Kiota generado) |
| Contrato público | `docs/contracts/bff.openapi.json` (pass-through v1) |
| Prefijo browser | `/api/` (nginx strip → `bff-api:8080`) |
| Auth SIWE | **Fuera del BFF** — `/auth/` directo a `auth-api` |
| Downstream v1 | verifier, issuer (holder + admin), academy, identity (health) |
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
