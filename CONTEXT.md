# SovereignID — Contexto del monorepo

Este repositorio es el **monorepo SovereignID**: plataforma de identidad soberana con microservicios independientes desplegables en contenedor.

> **Desambiguación:** el nombre *SovereignID* también aparece en repositorios legados y documentación histórica. **Este repo** es el monorepo greenfield descrito aquí; no asumir paridad de rutas ni código con implementaciones anteriores bajo `src/bc-auth/` u otros árboles legados.

## Mapa del monorepo

```
SovereignID/
├── SovereignID.sln          # Solución raíz
├── CONTEXT.md               # Este archivo
├── docs/                    # Contratos y documentación transversal
│   ├── adr/                 # Architecture Decision Records
│   └── contracts/           # OpenAPI snapshots y fixtures JSON
├── openspec/                # Cambios y specs OpenSpec
├── src/
│   └── auth/                # Microservicio de autenticación SIWE
│       ├── Auth.Api/        # HTTP, DI, configuración
│       ├── Auth.Application/# Casos de uso (IssueNonce, VerifySiwe)
│       ├── Auth.Domain/     # AuthChallenge, códigos de error
│       └── Auth.Infrastructure/ # Parser SIWE, store, JWT, firma
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

**Frontend (web):** los tipos TypeScript de request/response se mantienen **alineados manualmente** con el OpenAPI; CI valida alineación con **fixtures JSON** (respuestas de ejemplo) y **snapshot OpenAPI** versionado en `docs/contracts/`; además ejecuta `dotnet test` (AC-01…AC-07). Sin codegen automático. El estado de sesión del cliente usa el mismo nombre que el wire: **`jwt`** (no `token`). Tras verify exitoso, la **`address` de sesión proviene de la respuesta HTTP** (identidad certificada por auth), no de la lectura previa de la wallet. El cliente **persiste `expiresAt`** del JWT y restaura sesión solo si aún no ha caducado. El mensaje SIWE usa **chain ID Sepolia (`11155111`)** vía constante; v1 no comprueba ni fuerza el cambio de red en la wallet antes de firmar.

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

## Configuración relevante

| Variable / clave | Uso |
|------------------|-----|
| `AUTH_JWT_SIGNING_KEY` | Clave simétrica JWT (≥ 32 bytes UTF-8 fuera de Development) |
| `Auth:JwtIssuer` / `Auth:JwtAudience` | Claims `iss` y `aud` del JWT |
| `Auth:ChallengeTtlSeconds` | TTL del reto (default 600) |
| `Auth:JwtTtlHours` | TTL del JWT (default 24) |

## Ejecución local

```bash
dotnet run --project src/auth/Auth.Api
dotnet test tests/auth/Auth.IntegrationTests
```

Puerto HTTP de desarrollo: `http://localhost:5132` (ver `Properties/launchSettings.json`).
