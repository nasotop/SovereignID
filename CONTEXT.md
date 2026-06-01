# SovereignID — Contexto del monorepo

Este repositorio es el **monorepo SovereignID**: plataforma de identidad soberana con microservicios independientes desplegables en contenedor.

> **Desambiguación:** el nombre *SovereignID* también aparece en repositorios legados y documentación histórica. **Este repo** es el monorepo greenfield descrito aquí; no asumir paridad de rutas ni código con implementaciones anteriores bajo `src/bc-auth/` u otros árboles legados.

## Mapa del monorepo

```
SovereignID/
├── SovereignID.sln          # Solución raíz
├── CONTEXT.md               # Este archivo
├── docs/                    # Contratos y documentación transversal
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

Contrato observable: [`docs/siwe-backend-contract.md`](docs/siwe-backend-contract.md).

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
