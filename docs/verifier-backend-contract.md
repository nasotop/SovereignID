# Contrato de dominio — Servicio Verifier

Documento de referencia para la semántica de verificación pública de credenciales en `Verifier.Api`. Complementa el contrato HTTP en [`verifier.openapi.json`](contracts/verifier.openapi.json).

## Veredicto (`result`)

Valores emitidos en `POST /verifications` (campo `result`, snake_case):

| Valor | Significado |
|-------|-------------|
| `valid` | Credencial encontrada, no revocada, no expirada y sin fallos de evidencia evaluados |
| `revoked` | Revocada en BD y/o on-chain |
| `expired` | Vencida según `expires_at` o estado `expired` |
| `not_found` | UUID no existe en el índice interno |
| `integrity_failed` | Existe y no está revocada/expirada, pero al menos un chequeo de evidencia **evaluado** (no `null`) devolvió `false` |

### Precedencia

De mayor a menor prioridad:

`not_found` > `revoked` > `expired` > `integrity_failed` > `valid`

Un chequeo en `null` (no evaluado) **nunca** dispara `integrity_failed` por sí solo.

## Chequeos (`checks`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `found` | `bool?` | La credencial existe en BD |
| `notRevoked` | `bool?` | No revocada (BD ∪ on-chain) |
| `notExpired` | `bool?` | No expirada |
| `hashMatches` | `bool?` | SHA-256 del documento IPFS coincide con `content_hash` |
| `onChainExists` | `bool?` | Registro on-chain existe **y** es coherente con BD |
| `signatureValid` | `bool?` | Firma EIP-712 válida según jerarquía de fuente |
| `validationSource` | `enum?` | Fuente del veredicto de firma (ver abajo) |
| `revocationSource` | `enum?` | Fuente(s) de revocación: `bd`, `on_chain`, `both` |

### Activación incremental

Tres flags independientes en configuración (`Verifier:Evidence`), todos `false` por defecto:

- `OnChainCheckEnabled` → `onChainExists`, contribuye a `revocationSource` on-chain
- `IpfsCheckEnabled` → `hashMatches`
- `SignatureCheckEnabled` → `signatureValid`, `validationSource`

Con todos deshabilitados, el comportamiento equivale a v1 (chequeos externos en `null`).

### Jerarquía de firma (fallback asimétrico)

1. Si `onChainExists = true`: el `issuer` on-chain es la única fuente que puede **confirmar** (`signatureValid = true`).
2. Si on-chain no está disponible o el registro es incoherente: fallback a `institutions.issuer_wallet_address`:
   - Firma coincide con BD → `signatureValid = null`, `validationSource = bd_fallback_inconclusive` (no confirma).
   - Firma no coincide → `signatureValid = false`, `validationSource = bd_fallback_rejected`.
   - Sin wallet en BD → `signatureValid = null`, `validationSource = not_evaluated`.
3. Firma ausente/malformada → `signatureValid = false` (dato corrupto).

### Revocación combinada

`notRevoked = !(bdRevoked || onChainRevoked)`. On-chain puede elevar a `revoked` aunque BD diga `active`. BD revocada nunca se “des-revoca” por on-chain.

## Errores de protocolo (Problem Details)

| HTTP | `error` | Cuándo |
|------|---------|--------|
| `400` | `invalid_credential_id` | `credentialId` ausente o no UUID |
| `429` | `rate_limit_exceeded` | Límite de tasa por IP en `POST /verifications` |

Los veredictos de negocio **no** usan códigos 4xx/5xx; se devuelven con `200` y `result`.

## Rate limiting

Token bucket por IP en `Verifier.Api`: capacidad 10, recarga 1 token/3s (configurable). Requiere `ForwardedHeadersMiddleware` con redes/proxies de confianza restringidos al stack interno.

## Auditoría

Cada intento persiste en `verification_logs` con los mismos valores de chequeos y fuentes que la respuesta HTTP (`signature_validation_source`, `revocation_source` incluidos).
