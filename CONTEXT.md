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
│   └── scaffold-auth-db.ps1 # Regenerar modelo EF (database-first)
├── openspec/                # Cambios y specs OpenSpec
├── src/
│   ├── auth/                # Microservicio de autenticación SIWE
│   │   ├── Auth.Api/        # HTTP, DI, configuración
│   │   ├── Auth.Application/# Casos de uso (IssueNonce, VerifySiwe)
│   │   ├── Auth.Domain/     # AuthChallenge, códigos de error
│   │   └── Auth.Infrastructure/ # Parser SIWE, JWT, firma; Persistence/ (Generated, Stores, Composition)
│   └── verifier/            # Microservicio verificador de Verifiable Credentials
│       ├── Verifier.Api/        # HTTP (POST /verifications), DI, Problem Details
│       ├── Verifier.Application/# Caso de uso de verificación, interfaces de store
│       ├── Verifier.Domain/     # Veredicto (VerificationResult), códigos de error
│       └── Verifier.Infrastructure/ # Modelo EF acotado, adapters EF, composición DI
└── tests/
    ├── auth/
    │   └── Auth.IntegrationTests/      # AC-01…AC-07 (Testcontainers Postgres)
    └── verifier/
        └── Verifier.IntegrationTests/  # Escenarios VER (Testcontainers Postgres)
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

**Frontend (web):** los tipos TypeScript de request/response se mantienen **alineados manualmente** con el OpenAPI; CI valida alineación con **fixtures JSON** (respuestas de ejemplo) y **snapshot OpenAPI** versionado en `docs/contracts/`; además ejecuta `dotnet test` (AC-01…AC-07). Sin codegen automático. El estado de sesión del cliente usa el mismo nombre que el wire: **`jwt`** (no `token`). Tras verify exitoso, la **`address` de sesión proviene de la respuesta HTTP** (identidad certificada por auth), no de la lectura previa de la wallet. El cliente **persiste `expiresAt`** del JWT y restaura sesión solo si aún no ha caducado. El mensaje SIWE usa **chain ID Sepolia (`11155111`)** vía constante; v1 no comprueba ni fuerza el cambio de red en la wallet antes de firmar.

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

El registro de servicios (proyecto, puerto de export, ruta de snapshot) está en `scripts/openapi-lib.sh`. CI ejecuta el job **`verify-openapi`** que falla si algún snapshot está desactualizado; tras cambiar un contrato hay que reexportar y commitear el JSON.

## Base de datos y persistencia

PostgreSQL 16 en Docker (`docker-compose.yml`, servicio `postgres`). El esquema se aplica al primer arranque desde `database/BBDD_SovereignID.sql`.

| Aspecto | Regla |
|---------|--------|
| Fuente de verdad | `database/BBDD_SovereignID.sql` — no EF Code-First |
| Modelo .NET | Database-first con EF Core Power Tools (`scripts/scaffold-auth-db.ps1`) |
| Consumo en código | Interfaces en Application → adapters en Infrastructure |
| DbContext | `internal` en Infrastructure; no inyectar en Api ni casos de uso |
| Proveedor | Persistencia única respaldada por EF/Npgsql; `ConnectionStrings:DefaultConnection` siempre obligatoria (sin toggle `Persistence:Provider`) |
| Modelo por servicio | Cada microservicio tiene su propio modelo EF acotado a las tablas que consume (auth: 12 tablas; verifier: 4 tablas). No se comparte el modelo generado |

Decisiones completas: [ADR-0002](docs/adr/0002-database-consumption.md) (database-first, seams) y [ADR-0003](docs/adr/0003-single-ef-persistence.md) (persistencia única EF; retiro de `InMemory`).

Layout en `Auth.Infrastructure/Persistence/`:

| Zona | Carpeta | Contenido |
|------|---------|-----------|
| Generada (efcpt) | `Generated/` | `SovereignIdDbContext`, entidades EF en `Generated/Entities/` |
| Adapters | `Stores/<Aggregate>/` | Implementaciones de interfaces Application (p. ej. `ChallengeStore/`) |
| Composición DI | `Composition/` | `AddAuthPersistence()`, opciones, registro Npgsql |

**Stack local:**

```bash
docker compose up -d postgres            # levantar solo BD
.\scripts\scaffold-auth-db.ps1         # regenerar modelo EF de auth (requiere postgres healthy)
.\scripts\scaffold-verifier-db.ps1     # regenerar modelo EF del verifier (4 tablas)
```

Connection string desde contenedor: `Host=postgres;Port=5432;Database=sovereignid;Username=sovereignid;Password=sovereignid_dev`. Ver `.env.example`.

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
| **Verificación** | Operación de comprobar la validez de una VC por su UUID; crea un recurso auditado en `verification_logs` |
| **Verificador** | Tercero (típicamente anónimo) que solicita una verificación; no requiere autenticación |
| **Veredicto** | Resultado de una verificación: un `result` resumido más los booleanos `checks` (`found`, `notRevoked`, `notExpired`, …) |
| **`result`** | Valor estable del veredicto ∈ { `valid`, `revoked`, `expired`, `not_found` } en v1; `invalid_signature`, `tampered`, `ipfs_unreachable` reservados |

## Configuración relevante

| Variable / clave | Uso |
|------------------|-----|
| `AUTH_JWT_SIGNING_KEY` | Clave simétrica JWT (≥ 32 bytes UTF-8 fuera de Development) |
| `Auth:JwtIssuer` / `Auth:JwtAudience` | Claims `iss` y `aud` del JWT |
| `Auth:ChallengeTtlSeconds` | TTL del reto (default 600) |
| `Auth:JwtTtlHours` | TTL del JWT (default 24) |
| `ConnectionStrings:DefaultConnection` | Cadena Npgsql; **siempre obligatoria** (auth y verifier) |
| `POSTGRES_*` | Variables Docker del servicio `postgres` (ver `.env.example`) |

## Ejecución local

```bash
dotnet run --project src/auth/Auth.Api
dotnet run --project src/verifier/Verifier.Api
dotnet test tests/auth/Auth.IntegrationTests        # requiere Docker (Testcontainers)
dotnet test tests/verifier/Verifier.IntegrationTests # requiere Docker (Testcontainers)
```

Puerto HTTP de desarrollo (auth): `http://localhost:5132` (ver `Properties/launchSettings.json`).
