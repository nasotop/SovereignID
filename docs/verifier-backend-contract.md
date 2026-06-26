# Contrato backend — Verificación de Verifiable Credentials

> Verifier bounded context · Monorepo SovereignID

## 1. Propósito y alcance

Este documento define el **contexto de negocio y el contrato observable** del endpoint de verificación pública de Verifiable Credentials (VC) del servicio `verifier`. Describe el comportamiento esperado **sin** prescribir detalles internos de capas o archivos. Glosario: [`../CONTEXT.md`](../CONTEXT.md).

### Incluido (v1)

- Flujo de verificación (UUID → resolución → veredicto → log).
- Contrato HTTP de `POST /verifications`.
- Reglas de dominio: veredicto escalonado, precedencia, cómputo de expiración.
- Forma de la respuesta (`result`, `checks`, bloque `credential`).
- Registro de cada intento en `verification_logs`.
- Catálogo de errores y criterios de aceptación VER-01…VER-08.

### Excluido (v1)

- Chequeos con dependencia externa: fetch IPFS (`hashMatches`), consulta on-chain Sepolia (`onChainExists`), verificación de firma EIP-712 (`signatureValid`/`tampered`). Se devuelven `null` y quedan reservados.
- Envío de la VC completa (JSON-LD) para verificación offline.
- Entrada por IPFS CID.
- Rate limiting / anti-abuso del endpoint anónimo.
- Resolución DID on-chain.
- Endpoints de emisión o revocación.

---

## 2. Stack mínimo acordado

| Requisito | Valor |
|-----------|--------|
| Runtime | **.NET 10** |
| Host | **ASP.NET Core Web API** |
| Persistencia | **PostgreSQL** (Npgsql/EF Core), modelo database-first acotado a 4 tablas |
| Criterios de aceptación | Escenarios **VER-01…VER-08** (sección 9); ejecutados con xUnit + `WebApplicationFactory` + **Testcontainers PostgreSQL** |

---

## 3. Flujo de negocio (VC-01)

**Actor:** verificador (tercero, típicamente anónimo) que recibe un UUID de credencial (p. ej. escaneando un QR).
**Objetivo:** comprobar de forma independiente la validez de una VC sin confiar en la institución emisora.

```
1. El verificador obtiene el credentialId (UUID) de la VC      → del QR / referencia pública
2. El cliente llama al backend                                 → POST /verifications { credentialId }
3. Backend:
   a. Valida que credentialId es un UUID bien formado          → si no, 400 invalid_credential_id
   b. Resuelve la fila `credentials` por id (+ tipo + emisor)
   c. Computa los chequeos sin red: found, notRevoked, notExpired
   d. Determina result con la precedencia de veredicto
   e. Registra el intento en `verification_logs` (siempre, incluido not_found)
   f. Devuelve 200 con result + checks + bloque credential (o null)
4. El cliente muestra el veredicto al verificador
```

**Propiedades de negocio:**

- La verificación es **pública**: no requiere bearer token ni autenticación.
- Un veredicto negativo (revocada/expirada/inexistente) es un **resultado legítimo**, no un fallo de protocolo → `200 OK`.
- Toda verificación es **auditable**: deja rastro en `verification_logs`.

---

## 4. Contrato HTTP

### 4.1 `POST /verifications`

Verifica una VC por su UUID.

**Cuerpo (JSON)**

```json
{
  "credentialId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

| Campo | Tipo | Regla |
|-------|------|--------|
| `credentialId` | string | UUID (`credentials.id`). Obligatorio. Formato UUID válido. |

**Respuesta 200 — credencial válida**

```json
{
  "result": "valid",
  "checks": {
    "found": true,
    "notRevoked": true,
    "notExpired": true,
    "hashMatches": null,
    "onChainExists": null,
    "signatureValid": null
  },
  "credential": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "type": "TITULO",
    "status": "active",
    "issuedAt": "2026-01-15T12:00:00Z",
    "expiresAt": null,
    "subjectDid": "did:ethr:sepolia:0x…",
    "issuer": {
      "did": "did:ethr:sepolia:0x…",
      "displayName": "Universidad de Ejemplo",
      "code": "UDE"
    },
    "anchors": {
      "ipfsCid": "bafybeig…",
      "contentHash": "0x…",
      "transactionHash": "0x…",
      "chainId": 11155111
    }
  }
}
```

**Respuesta 200 — credencial inexistente**

```json
{
  "result": "not_found",
  "checks": {
    "found": false,
    "notRevoked": null,
    "notExpired": null,
    "hashMatches": null,
    "onChainExists": null,
    "signatureValid": null
  },
  "credential": null
}
```

| Campo | Tipo | Regla |
|-------|------|--------|
| `result` | string | Veredicto resumido (sección 6). |
| `checks` | object | Booleanos por chequeo; `null` = no evaluado. |
| `credential` | object \| null | Datos de la credencial resuelta; `null` si `not_found`. |

**Errores de entrada:** ver sección 8.

---

## 5. Modelo de respuesta — detalle

### 5.1 `checks`

| Chequeo | v1 | Significado |
|---------|----|-------------|
| `found` | computado | La credencial existe en `credentials`. |
| `notRevoked` | computado | No está revocada (ver 6.2). `null` si `not_found`. |
| `notExpired` | computado | No está expirada (ver 6.3). `null` si `not_found`. |
| `hashMatches` | `null` | Reservado: el hash IPFS coincide con el on-chain. |
| `onChainExists` | `null` | Reservado: la credencial existe en la blockchain. |
| `signatureValid` | `null` | Reservado: firma EIP-712 válida. |

### 5.2 Bloque `credential`

Presente cuando la credencial existe (cualquier `result` ≠ `not_found`). Incluye `type` (código del `credential_type`), `status`, `issuedAt`, `expiresAt`, `subjectDid`, el sub-objeto `issuer` (datos de `institutions`) y `anchors` (anclas on-chain/IPFS). `null` cuando `result = not_found`.

---

## 6. Reglas de dominio

### 6.1 Veredicto escalonado v1

Solo se computan los chequeos **sin dependencia externa** (`found`, `notRevoked`, `notExpired`) contra la BD. Los chequeos con red (`hashMatches`, `onChainExists`, `signatureValid`) se devuelven `null`.

### 6.2 Revocación

Una credencial se considera **revocada** cuando:

```
status = 'revoked'  OR  revoked_at IS NOT NULL
```

### 6.3 Expiración

Una credencial se considera **expirada** cuando:

```
(expires_at IS NOT NULL AND expires_at < now)  OR  status = 'expired'
```

El instante actual (`now`) se obtiene vía `TimeProvider` en **UTC**. La expiración se **computa desde `expires_at`** y no se confía solo en `status` (evita el bug del job nocturno que no corrió y dejó `status = 'active'` con `expires_at` ya pasado).

### 6.4 Precedencia del veredicto

```
not_found  >  revoked  >  expired  >  valid
```

- `not_found`: no existe fila para el UUID.
- `revoked`: existe y está revocada (gana sobre expirada — la revocación es un acto intencional, señal más fuerte).
- `expired`: existe, no revocada, pero expirada.
- `valid`: existe, no revocada, no expirada.

### 6.5 Registro del intento

Cada llamada inserta una fila en `verification_logs`:

| Columna | Valor |
|---------|-------|
| `credential_id` | UUID de la credencial, o **`NULL`** si `not_found` |
| `credential_id_query` | UUID consultado (siempre, incluso si no existe) |
| `result` | el `result` del veredicto (enum `verification_result`) |
| `not_revoked` / `not_expired` | booleanos computados (o `NULL` si no evaluados) |
| `signature_valid` / `hash_matches` / `on_chain_exists` | **`NULL`** (no evaluados en v1) |
| `verifier_ip` | IP del verificador (anónimo) |

---

## 7. Persistencia

- Modelo EF propio del verifier, **database-first**, acotado a `credentials`, `verification_logs`, `credential_types`, `institutions`. `DbContext` (`VerifierDbContext`) `internal`, independiente del de `auth`.
- Acceso vía interfaces de Application: `ICredentialReadStore` (lectura credencial + tipo + emisor) e `IVerificationLogStore` (escritura del intento). Un adapter EF por interfaz (`EfCredentialReadStore`, `EfVerificationLogStore`).
- Postgres-only: `ConnectionStrings:DefaultConnection` obligatoria. Sin proveedor InMemory ni toggle. Ver [ADR-0003](adr/0003-single-ef-persistence.md).

---

## 8. Catálogo de errores

Errores de **entrada/protocolo** como **Problem Details** (RFC 7807, `application/problem+json`) con extensión `error`. Los veredictos de negocio **no** son errores: usan `200 OK`.

| Código | HTTP | Cuándo |
|--------|------|--------|
| `invalid_credential_id` | 400 | `credentialId` ausente o con formato de UUID inválido |

El endpoint **MUST NOT** usar `401` ni `404` para veredictos de negocio.

---

## 9. Criterios de aceptación

Definen el comportamiento observable correcto para v1.

### VER-01 · Credencial válida

- **Dado** un `credentialId` de una credencial existente, no revocada y no expirada
- **Cuando** se llama a `POST /verifications`
- **Entonces** `200`, `result = valid`, `checks.found/notRevoked/notExpired = true`, bloque `credential` poblado (con `issuer` y `anchors`)

### VER-02 · Credencial revocada

- **Dado** una credencial con `status = 'revoked'` o `revoked_at` no nulo
- **Entonces** `200`, `result = revoked`, `checks.notRevoked = false`

### VER-03 · Credencial expirada por fecha

- **Dado** una credencial con `status = 'active'` pero `expires_at` anterior a ahora
- **Entonces** `200`, `result = expired`, `checks.notExpired = false`

### VER-04 · Credencial inexistente

- **Dado** un `credentialId` (UUID válido) que no corresponde a ninguna fila
- **Entonces** `200`, `result = not_found`, `checks.found = false`, `credential = null`

### VER-05 · Revocada y expirada (precedencia)

- **Dado** una credencial revocada **y** expirada simultáneamente
- **Entonces** `200`, `result = revoked` (la revocación tiene precedencia)

### VER-06 · Chequeos externos no evaluados

- **Cuando** cualquier verificación se ejecuta en v1
- **Entonces** `checks.hashMatches`, `checks.onChainExists` y `checks.signatureValid` son `null`

### VER-07 · UUID malformado

- **Cuando** `credentialId` no es un UUID válido
- **Entonces** `400` Problem Details con `error = invalid_credential_id`

### VER-08 · `credentialId` ausente

- **Cuando** el cuerpo no incluye `credentialId`
- **Entonces** `400` Problem Details con `error = invalid_credential_id`

### VER-LOG · Auditoría de todo intento

- **Cuando** se ejecuta cualquier verificación (incluido `not_found`)
- **Entonces** se inserta una fila en `verification_logs` con `credential_id_query` = UUID consultado; `credential_id = NULL` en `not_found`
