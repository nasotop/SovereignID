# Evolución del modelo de base de datos — SovereignID

Documento de resumen: cambios ejecutados sobre el esquema PostgreSQL desde el diseño MVP inicial, y la justificación de cada uno.

**Fuente de verdad del esquema:** `database/BBDD_SovereignID.sql`  
**Parches incrementales (BDs existentes):** `database/patches/`  
**Reglas de consumo:** [ADR-0002](adr/0002-database-consumption.md)

---

## 1. Contexto

SovereignID usa **un PostgreSQL 16 compartido** para todos los microservicios del MVP. El esquema es **database-first**: se edita el SQL canónico y, si hace falta, un patch idempotente; no se usan migraciones EF Code-First, Flyway ni Liquibase.

La BD actúa como **índice operativo** del sistema de credenciales verificables:

- Guarda metadatos, relaciones multi-tenant, roles y anclas (IPFS / on-chain).
- **No almacena PII académica** en tablas de estudiantes ni en el cuerpo de la credencial: esos datos viven en el JSON-LD en IPFS o en el SIS de la institución.

---

## 2. Modelo base (nacimiento del esquema)

El diseño inicial unificó el MVP en un solo script SQL. Conceptualmente se documentó como **~12 tablas**; el canónico actual integra también las extensiones posteriores y llega a **15 tablas** y **5 ENUMs**.

### Tablas del núcleo MVP

| Grupo | Tablas | Justificación |
|-------|--------|---------------|
| Catálogo | `credential_types` | Tipos de VC (TÍTULO, NOTAS, etc.) con contexto JSON-LD |
| Tenant | `institutions`, `careers` | Multi-institución y carreras por tenant |
| Identidad académica | `students`, `student_wallets` | Estudiante anónimo + historial de wallets (rotación sin invalidar VCs antiguas) |
| Identidad administrativa | `users`, `institution_users`, `institution_invitations` | Usuarios por wallet; RBAC institucional; invitaciones con hash del token (no token en claro) |
| Credenciales | `credentials` | Índice de VCs: CID IPFS, `content_hash`, tx Sepolia, firma EIP-712 |
| Verificación | `verification_logs` | Trazabilidad de cada intento del verifier público |
| Auth | `auth_challenges` | Retos SIWE de un solo uso |
| Observabilidad | `audit_logs`, `institution_metrics_daily` | Auditoría administrativa y KPIs diarios (diseño anticipado) |

### Decisiones fundacionales vigentes

1. **PII fuera de BD** — `students` sin nombre/RUT; datos personales en IPFS o SIS.
2. **Sepolia fija** — `chain_id = 11155111` en credenciales y challenges.
3. **Wallet dual-role** — la misma address puede ser titular (`student_wallets`) y usuario institucional (`institution_users`).
4. **SQL como fuente de verdad** — regeneración EF con `efcpt` tras cambios de esquema.

---

## 3. Cambios ejecutados (cronología)

### 3.1 Autorización formalizada — 2026-07-04

**Archivo:** `database/patches/2026-07-04-academy-authz.sql`  
**Estado:** integrado en `BBDD_SovereignID.sql`

| Cambio | Detalle |
|--------|---------|
| Nuevo ENUM `global_user_role` | Valor `platform_admin` |
| Nueva tabla `user_global_roles` | Roles de plataforma por usuario, con soft-revoke (`revoked_at`) |
| Extensión ENUM `user_role` | Nuevo valor `viewer` |

**Justificación**

- Separar el rol de **plataforma** del rol **institucional**.
- Persistir `platform_admin` en BD (no solo allowlist en configuración de auth).
- Habilitar un perfil de **solo lectura** institucional (`viewer`) para reportes y dashboards en `/academy`, sin poder emitir ni administrar.

**Disparador:** matriz RBAC (`authorization-domain-contract`) y reportes institution/platform (`add-reports-api`).  
**Consumidores:** `auth` (claims JWT en login), `reports` (policies de acceso).

---

### 3.2 Perfil off-chain del titular — 2026-07-05

**Archivo:** `database/patches/2026-07-05-holder-profile.sql`  
**Estado:** integrado en el canónico

| Cambio | Detalle |
|--------|---------|
| Nueva tabla `holder_profiles` | PK = `user_id` → `users`; nombre, email, teléfono, país, fecha de nacimiento |

**Justificación**

- El portal del titular necesita datos de contacto editables en UI.
- Esos datos **no deben anclarse en blockchain** ni mezclarse con el registro anónimo `students`.
- Alineado con el **reparto identitario v1** ([ADR-0006](adr/0006-consolidate-identity-into-academy-auth.md)): perfil holder en `academy`, sin microservicio `identity`.

**Regla de dominio:** el claim JWT `holder` se resuelve desde `student_wallets` (wallet primaria activa), **no** desde `holder_profiles`.  
**Consumidor:** `academy` (`GET/PUT /academy/holders/me`).

---

### 3.3 Evidencia criptográfica en verificación — 2026-07-06

**Archivo:** `database/patches/2026-07-06-verifier-evidence-checks.sql`  
**Estado:** integrado en el canónico

| Cambio | Detalle |
|--------|---------|
| ENUM `verification_result` | Nuevo valor `integrity_failed` |
| Columna `signature_validation_source` | Fuente del veredicto de firma (`on_chain`, `bd_fallback_*`, `not_evaluated`) |
| Columna `revocation_source` | Quién reportó revocación (`bd`, `on_chain`, `both`) |

**Justificación**

- El verifier pasó de cruzar solo el índice interno a evaluar evidencia real (contrato on-chain, IPFS, firma EIP-712).
- Hace falta un veredicto distinto cuando un chequeo evaluado falla (`integrity_failed`).
- Las columnas de fuente permiten **auditar** de dónde salió cada conclusión (jerarquía asimétrica: on-chain confirma; BD solo puede rechazar o quedar inconclusa).

**Disparador:** cambio OpenSpec `add-verifier-evidence-checks`.  
**Consumidores:** `verifier` (escribe logs), `reports` (agrega verificaciones).

---

### 3.4 Cambios sin DDL (en curso / relacionados)

| Cambio | Impacto en esquema | Justificación |
|--------|--------------------|---------------|
| `add-issuer-content-anchor` | Solo datos seed (`seed-dev.sql`) | La columna `content_hash` ya existía; se refuerza integridad en aplicación y fixtures |
| `add-frontend-content-anchor` | Ninguno | UI sobre contrato existente |

---

## 4. Balance: de ~12 a 15 tablas

| Evolución | Qué cambió | Motivo de negocio |
|-----------|------------|-------------------|
| Base MVP | Núcleo multi-tenant + credenciales + auth + observabilidad | Índice de VCs sin PII |
| 2026-07-04 | `+ user_global_roles`, `viewer` | RBAC de plataforma y solo lectura |
| 2026-07-05 | `+ holder_profiles` | Perfil off-chain del titular |
| 2026-07-06 | Δ `verification_logs` + `integrity_failed` | Verificación criptográfica auditable |

---

## 5. Quién consume qué (mapa resumido)

| Microservicio | Tablas principales | Rol |
|---------------|--------------------|-----|
| `auth` | `auth_challenges`, `users`, `user_global_roles`, `institution_users`, `student_wallets` | SIWE, JWT, resolución de roles |
| `academy` | `institutions`, `careers`, `students`, `student_wallets`, `users`, `institution_users`, `institution_invitations`, `holder_profiles` | Tenant, alumnos, invitaciones, perfil holder |
| `issuer` | `credentials`, `credential_types`, `institutions`, `students`, `student_wallets`, `careers` | Emisión y consulta de VCs |
| `verifier` | `credentials`, `verification_logs` (+ joins de lectura) | Verificación pública y auditoría de intentos |
| `reports` | `institutions`, `students`, `credentials`, `verification_logs`, `institution_metrics_daily` | KPIs institution/platform |

**Pendiente de uso en código:** `audit_logs` (tabla diseñada; sin adapter en MVP).

---

## 6. Cómo se aplican los cambios

1. Actualizar `database/BBDD_SovereignID.sql` (instalaciones nuevas / reset Docker).
2. Añadir patch idempotente en `database/patches/` para bases ya existentes.
3. Regenerar modelos EF con los scripts `scaffold-*-db.ps1` donde aplique.
4. Consumir solo vía interfaces Application + adapters Infrastructure ([ADR-0002](adr/0002-database-consumption.md)).

---

## 7. Conclusión

La evolución del modelo no es ad-hoc: cada delta responde a una capacidad de producto documentada (RBAC, portal holder, evidencia de verificación). El esquema pasó de un índice MVP de credenciales a un modelo que también sostiene **autorización de plataforma**, **perfil off-chain del titular** y **trazabilidad criptográfica de verificaciones**, sin romper el principio de no almacenar PII académica en la base relacional.
