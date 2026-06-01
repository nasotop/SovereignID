## Context

Monorepo SovereignID en greenfield: un microservicio (`auth`) con plantilla ASP.NET Core 10 en `src/Auth.API` (ubicación incorrecta). El contrato observable vive en `docs/siwe-backend-contract.md` (§§3–10). No existe `CONTEXT.md`. Despliegue inicial: un contenedor por servicio; store de retos en memoria. Futuros microservicios seguirán `src/{servicio}/` con carpetas en minúscula.

## Goals / Non-Goals

**Goals:**

- Estructura monorepo: `src/auth/{Auth.Api, Auth.Application, Auth.Domain, Auth.Infrastructure}`, `tests/auth/Auth.IntegrationTests`, `SovereignID.sln` en raíz.
- `CONTEXT.md` con mapa de servicios, glosario SIWE y nota de desambiguación del nombre de repo.
- Implementar flujo SC-01: nonce → firma SIWE → verify → JWT 24h.
- Cumplir AC-01…AC-07 mediante tests de integración (`WebApplicationFactory` + Nethereum para firmar en tests).
- Errores RFC 7807 con códigos `snake_case` estables del contrato.

**Non-Goals:**

- Endpoints distintos de `/auth/nonce` y `/auth/verify` (p. ej. `/auth/me`).
- Frontend, VC, resolución DID on-chain, multi-chain.
- Store distribuido (Redis/SQL) — v1 in-memory.
- Prescripción de minimal APIs vs controllers (equivalente para el contrato).

## Decisions

### 1. Layout monorepo por servicio

**Decisión:** `src/auth/` agrupa todas las capas; `tests/auth/` las pruebas del servicio.

**Alternativa:** Proyectos en raíz de `src/` (`src/Auth.Api`) — rechazada: no escala al segundo microservicio.

### 2. Capas clásicas (sin bounded context DDD)

| Proyecto | Responsabilidad |
|----------|-----------------|
| `Auth.Domain` | `AuthChallenge`, invariantes, códigos de error de dominio |
| `Auth.Application` | `IssueNonce`, `VerifySiwe` — orden del pipeline |
| `Auth.Infrastructure` | Parser SIWE, store in-memory, verificación firma, JWT HS256 |
| `Auth.Api` | HTTP, DTOs, Problem Details, DI, configuración |

**Alternativa:** Proyecto único — rechazada por mantenibilidad y futuros servicios.

### 3. Store de challenges en memoria

**Decisión:** `ConcurrentDictionary<string, AuthChallenge>` con comprobación de caducidad al leer y marca atómica de consumido.

**Alternativa:** Redis — pospuesta hasta réplicas múltiples.

### 4. Pipeline de verify (orden contractual)

1. Parse SIWE → conservar `OriginalPayload` → `siwe_parse_failed`
2. Nonce existe → `nonce_unknown`
3. No caducado → `nonce_expired`
4. No consumido → `nonce_consumed`
5. Chain ID `11155111` → `unsupported_chain`
6. Recuperar firma sobre `OriginalPayload` → `signature_mismatch`
7. Consumir + emitir JWT

### 5. Reloj inyectable

**Decisión:** `TimeProvider` (o abstracción equivalente) en Application/Infrastructure para AC-03 sin esperar 10 minutos reales.

### 6. Verificación de firma

**Decisión:** Recuperación EIP-191 `personal_sign` en Infrastructure; tests usan Nethereum `EthereumMessageSigner` con clave efímera.

### 7. JWT

**Decisión:** HS256, claims obligatorios (`sub`, `address`, `did`, `iat`, `exp`, `iss`, `aud`); clave desde `AUTH_JWT_SIGNING_KEY` (≥32 bytes fuera de Development).

**Alternativa:** RS256 — fuera de contrato v1.

### 8. HTTP en Api

**Decisión:** Controllers o minimal APIs — a elección del implementador; respuestas y status codes fijados por contrato.

### 9. Documentación

**Decisión:** Podar encabezado, §11 y referencias legado en `siwe-backend-contract.md`; fuente normativa para agentes: specs OpenSpec + contrato en `docs/`.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| Parser diverge de wallets (saltos `\n`, `\r\n`) | Normalizar `\r\n`→`\n`; tests con payload firmado real |
| Re-serializar mensaje rompe firma | Verificar siempre sobre `OriginalPayload` |
| In-memory pierde retos al reiniciar | Aceptado en v1 single container |
| Confusión nombre repo legado vs este | `CONTEXT.md` + README explícito |
| Clave JWT débil en dev | Validar longitud ≥32 bytes fuera de Development al arranque |

## Migration Plan

1. Crear estructura `src/auth/` y proyectos vacíos referenciados desde `SovereignID.sln`.
2. Mover contenido útil de `src/Auth.API` (Dockerfile, appsettings) a `src/auth/Auth.Api`.
3. Eliminar `src/Auth.API` y `Auth.API.slnx` local.
4. Implementar por capas siguiendo `tasks.md` (reestructura → dominio → HTTP → ACs).
5. Rollback: revertir commit de migración; no hay datos persistentes en v1.

## Open Questions

- ¿Controllers o minimal APIs en `Auth.Api`? (sin impacto en contrato)
- ¿Nethereum también en runtime de Infrastructure o solo en tests? (recomendado: misma lib en ambos para paridad de algoritmo)
