# Contrato backend — Sign-In with Ethereum (SIWE)

> Auth bounded context · Monorepo SovereignID
## 1. Propósito y alcance

Este documento define el **contexto de negocio y el contrato observable** del login backend con **Sign-In with Ethereum (EIP-4361)** en el monorepo SovereignID. Describe el comportamiento esperado del servicio bajo `src/auth/` **sin** prescribir detalles internos de capas o archivos concretos. Glosario: [`../CONTEXT.md`](../CONTEXT.md).

### Incluido (v1)

- Flujo de autenticación SIWE (reto → firma → verificación → sesión JWT).
- Contrato HTTP de `GET /auth/nonce` y `POST /auth/verify`.
- Reglas de dominio: nonce, caducidad, un solo uso, chain Sepolia, verificación de firma.
- Forma del mensaje SIWE que el backend acepta.
- Forma del JWT emitido.
- Catálogo de errores y criterios de aceptación del servicio auth v1.

### Excluido (v1)

- Demo estática / frontend (`wwwroot`).
- Endpoint `GET /auth/me`.
- Emisión o verificación de Verifiable Credentials.
- Resolución DID on-chain.
- Multi-chain configurable.
- Prescripción de capas, DI, proyectos ni librerías concretas (salvo stack mínimo indicado abajo).

---

## 2. Stack mínimo acordado

| Requisito | Valor |
|-----------|--------|
| Runtime | **.NET 10** |
| Host | **ASP.NET Core Web API** |
| Criterios de aceptación | Siete escenarios **AC-01…AC-07** descritos en la sección 10; ejecutados con xUnit + `WebApplicationFactory` y firma de test con **Nethereum** (`EthereumMessageSigner` + clave efímera) |

La organización interna del código (controllers, minimal APIs, carpetas, etc.) queda **fuera de este contrato**.

---

## 3. Flujo de negocio (SC-01)

**Actor:** titular con wallet Ethereum (p. ej. MetaMask).  
**Objetivo:** autenticarse sin contraseña ni proveedor centralizado.

```
1. Cliente solicita un reto al backend          → GET /auth/nonce
2. Backend emite nonce de un solo uso + expiry
3. Cliente construye mensaje EIP-4361         → incluye domain, address, chain id, nonce, …
4. Wallet firma el mensaje (personal_sign)      → off-chain, sin gas
5. Cliente envía mensaje + firma al backend     → POST /auth/verify
6. Backend:
   a. Parsea y valida el mensaje SIWE
   b. Comprueba que el nonce fue emitido y sigue vigente
   c. Comprueba chain id Sepolia
   d. Recupera la dirección desde la firma (EIP-191 / personal_sign)
   e. Compara dirección recuperada con la del mensaje
   f. Consume el nonce (un solo uso)
   g. Emite JWT de sesión (24 h)
7. Cliente usa el JWT como bearer token en llamadas posteriores
```

**Propiedades de negocio:**

- No se crea “cuenta” en base de datos: la identidad es la **dirección Ethereum** que controla la clave privada.
- El login es **off-chain**; no hay transacción en blockchain.
- Tras SIWE, el resto de la aplicación puede usar un **JWT convencional** sin re-verificar la firma en cada petición.

---

## 4. Contrato HTTP

### 4.1 `GET /auth/nonce`

Emite un **auth challenge** nuevo.

**Respuesta 200**

```json
{
  "nonce": "a1b2c3d4e5f6789012345678901234ab",
  "expiresAt": "2026-05-31T12:10:00Z"
}
```

| Campo | Tipo | Regla |
|-------|------|--------|
| `nonce` | string | Exactamente **32 caracteres hex minúsculas** (`[0-9a-f]{32}`), sin prefijo `0x`. Entropía: 128 bits. |
| `expiresAt` | string | Instantáneo UTC en formato ISO 8601 / RFC 3339 (p. ej. sufijo `Z`). |

**Semántica:** el backend **persiste** el reto asociado al nonce hasta que expire o se consuma con éxito. La duración por defecto es **600 segundos (10 minutos)** desde la emisión.

---

### 4.2 `POST /auth/verify`

Verifica un mensaje SIWE firmado y, si es válido, emite sesión JWT.

**Cuerpo (JSON)**

```json
{
  "message": "<payload SIWE completo, string multilínea>",
  "signature": "0x…"
}
```

| Campo | Tipo | Regla |
|-------|------|--------|
| `message` | string | Payload EIP-4361 **exactamente** como fue firmado (incluye saltos de línea). |
| `signature` | string | Firma hex con prefijo `0x`, producida con `personal_sign` sobre `message`. |

**Respuesta 200**

```json
{
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…",
  "address": "0xA0b86991c6218b36c1d19d4a2e9eb0cE3606eB48",
  "expiresAt": "2026-06-01T12:00:00Z"
}
```

| Campo | Tipo | Regla |
|-------|------|--------|
| `jwt` | string | Token JWT firmado (ver sección 8). |
| `address` | string | Dirección Ethereum recuperada de la firma (formato `0x` + 40 hex; en la respuesta puede reflejar checksum EIP-55 según implementación). |
| `expiresAt` | string | Caducidad del JWT en UTC (ISO 8601). |

**Errores:** ver sección 9. Los códigos de error son **estables** (`snake_case`) para consumo programático.

---

## 5. Mensaje SIWE (EIP-4361) — forma exigida

El backend parsea el payload **línea a línea** (saltos `\n`; `\r\n` se normalizan a `\n`). Desviaciones producen `siwe_parse_failed` (HTTP 400).

### 5.1 Líneas obligatorias (orden fijo)

| Línea | Formato | Ejemplo |
|-------|---------|---------|
| 1 | `{domain} wants you to sign in with your Ethereum account:` | `myapp.example wants you to sign in with your Ethereum account:` |
| 2 | `{0xAddress}` | `0xA0b86991c6218b36c1d19d4a2e9eb0cE3606eB48` |
| 3 | *(vacía)* | |
| 4 | `{statement}` | Texto libre (puede estar vacío en el parser, pero la línea existe) |
| 5 | *(vacía)* | |
| 6 | `URI: {uri absoluta}` | `URI: https://myapp.example` |
| 7 | `Version: 1` | Literal `Version: 1` |
| 8 | `Chain ID: {decimal}` | `Chain ID: 11155111` |
| 9 | `Nonce: {nonce}` | `Nonce: a1b2c3d4…` (32 hex minúsculas) |
| 10 | `Issued At: {ISO8601}` | `Issued At: 2026-05-31T12:00:00Z` |

Mínimo **10 líneas** en el payload.

### 5.2 Campos opcionales (después de la línea 10)

Si aparecen, deben seguir la gramática EIP-4361:

- `Expiration Time: {ISO8601}`
- `Not Before: {ISO8601}`
- `Request ID: {texto}`
- Bloque `Resources:` seguido de líneas `- {uri absoluta}`

Cualquier otra línea no vacía no reconocida → `siwe_parse_failed`.

### 5.3 Validaciones de negocio sobre el mensaje

| Regla | Error si falla |
|-------|----------------|
| Dirección línea 2: `0x` + 40 dígitos hex | `siwe_parse_failed` |
| `Version` distinta de `1` | `siwe_parse_failed` |
| `Chain ID` distinto de **11155111** (Sepolia) | `unsupported_chain` (400) — evaluado **después** de comprobar que el nonce existe |
| `Nonce` no coincide con un reto emitido | `nonce_unknown` (401) |
| Nonce caducado | `nonce_expired` (401) |
| Nonce ya consumido (reintento con mismo payload válido) | `nonce_consumed` (401) |

**Importante para la firma:** la verificación criptográfica debe aplicarse sobre el **payload original intacto** (`OriginalPayload`), no sobre una re-serialización del mensaje parseado.

**Importante para wallets:** el campo `domain` (línea 1) debe coincidir con el origen desde el que el usuario inicia el login; MetaMask usa esto como señal anti-phishing.

---

## 6. Reglas de dominio

### 6.1 Auth challenge

Un **auth challenge** agrupa:

- el **nonce** emitido,
- instante de **emisión**,
- instante de **caducidad** (`issuedAt + TTL`),
- estado **consumido** (sí/no).

**Invariantes:**

1. Cada nonce emitido por `GET /auth/nonce` es **único** en el almacén de retos activos.
2. TTL del reto: **600 s** por defecto (configurable; ver sección 9).
3. Un reto **válido** solo puede consumirse **una vez** con verificación exitosa.
4. Tras consumo exitoso, un segundo intento con el **mismo** mensaje y firma válidos debe responder **`nonce_consumed`**, no `nonce_unknown`. *(El reto consumido permanece clasificable como “ya usado” hasta su eviction por caducidad.)*
5. Un nonce **nunca emitido** → `nonce_unknown`.
6. Un nonce **emitido pero pasada su `expiresAt`** → `nonce_expired`.

### 6.2 Verificación de firma

| Paso | Regla |
|------|--------|
| Algoritmo | Recuperación de dirección desde firma **`personal_sign`** (prefijo EIP-191 sobre el UTF-8 del mensaje). |
| Comparación | Dirección en mensaje SIWE vs dirección recuperada, **case-insensitive** (recomendado: normalizar ambas a EIP-55 antes de comparar). |
| Desajuste | `signature_mismatch` (401). |
| Firma válida para **otro** payload (mensaje alterado) | `signature_mismatch` (401). |

### 6.3 Chain policy

Solo se acepta **Ethereum Sepolia**:

- **Chain ID:** `11155111`
- Cualquier otro valor → `unsupported_chain` (400)

Motivo de negocio: evitar replay de firmas destinadas a otra red (p. ej. mainnet) contra un backend configurado para Sepolia.

### 6.4 Sesión post-login

Tras verificación exitosa:

1. Se **consume** el nonce.
2. Se emite un **JWT** con TTL **24 horas** (fijo en la implementación de referencia).

No se exige endpoint protegido por JWT en v1; el token es el artefacto de sesión entregado al cliente.

---

## 7. Configuración (requisitos de producto)

Valores por defecto del servicio auth; **`iss`, `aud` y clave de firma son configurables**, pero deben cumplir las reglas de seguridad.

| Parámetro | Default | Regla |
|-----------|---------|--------|
| TTL nonce | `600` s | Duración del auth challenge |
| TTL JWT | `24` h | Duración de sesión |
| Chain ID aceptado | `11155111` | Fijo en v1 |
| Clave JWT | variable de entorno `AUTH_JWT_SIGNING_KEY` | **Obligatoria** fuera de Development; **≥ 32 bytes** UTF-8 |
| Emisor JWT (`iss`) | `sovereignid-auth` | Configurable |
| Audiencia JWT (`aud`) | `sovereignid-clients` | Configurable |

No se normativiza **cómo** leer estos valores en .NET (`appsettings`, variables de entorno, etc.).

---

## 8. JWT emitido

### 8.1 Firma

- Algoritmo: **HMAC-SHA256** (`HS256`).
- Clave simétrica: ver `AUTH_JWT_SIGNING_KEY`.

### 8.2 Claims

| Claim | Obligatorio | Contenido |
|-------|-------------|-----------|
| `sub` | sí | Dirección Ethereum en **minúsculas** (`0x` + 40 hex) |
| `address` | sí | Mismo valor que `sub` (explícito para consumidores downstream) |
| `did` | sí | `did:ethr:sepolia:{sub}` — derivación string, **sin** resolución on-chain en v1 |
| `iat` | sí | Emisión (segundos Unix) |
| `exp` | sí | Caducidad (24 h después de emisión) |
| `iss` | sí | Configurable |
| `aud` | sí | Configurable |

### 8.3 Interoperabilidad

La **forma de claims** debe mantenerse estable para integración con consumidores downstream.

---

## 9. Catálogo de errores

Las respuestas de error usan **Problem Details** (RFC 7807). Campos observables:

| Campo | Descripción |
|-------|-------------|
| `title` | `"Authentication failed"` |
| `status` | `400` o `401` |
| `detail` | Texto legible para humanos |
| `error` | Código estable (`snake_case`) |

### 9.1 Códigos

| Código | HTTP | Cuándo |
|--------|------|--------|
| `siwe_parse_failed` | 400 | Payload SIWE malformado o línea no reconocida |
| `unsupported_chain` | 400 | `Chain ID` ≠ 11155111 |
| `nonce_unknown` | 401 | Nonce no emitido por este backend |
| `nonce_expired` | 401 | Reto caducado |
| `nonce_consumed` | 401 | Reintento tras login exitoso con el mismo reto |
| `signature_mismatch` | 401 | Firma no corresponde al mensaje o a la dirección declarada |

---

## 10. Criterios de aceptación

Estos escenarios definen el comportamiento observable correcto para v1.

### AC-01 · Happy path

- **Dado** un wallet con cuenta Ethereum
- **Cuando** se obtiene nonce, se firma un mensaje SIWE válido (Sepolia, nonce correcto) y se llama a `POST /auth/verify`
- **Entonces** HTTP 200, cuerpo con `jwt`, `address` y `expiresAt`
- **Y** el claim `sub` del JWT coincide con la dirección firmante en minúsculas

### AC-02 · Replay

- **Dado** un verify exitoso con un par `(message, signature)` válido
- **Cuando** se repite el mismo `POST /auth/verify`
- **Entonces** HTTP 401, `error` = `nonce_consumed`

### AC-03 · Nonce caducado

- **Dado** un nonce emitido
- **Cuando** transcurre más de **10 minutos** (TTL del reto) antes del verify
- **Entonces** HTTP 401, `error` = `nonce_expired`

### AC-04 · Chain incorrecta

- **Dado** un nonce emitido
- **Cuando** el mensaje SIWE declara `Chain ID: 1` (u otro distinto de 11155111) pero firma y nonce son coherentes
- **Entonces** HTTP 400, `error` = `unsupported_chain`

### AC-05 · Mensaje alterado

- **Dado** un nonce emitido y una firma válida para el mensaje **original**
- **Cuando** se envía un mensaje **modificado** con la firma del original
- **Entonces** HTTP 401, `error` = `signature_mismatch`

### AC-06 · Nonce desconocido

- **Dado** un nonce emitido `N`
- **Cuando** el mensaje SIWE incluye un nonce `N'` ≠ `N` (nunca emitido)
- **Entonces** HTTP 401, `error` = `nonce_unknown`

### AC-07 · Payload malformado

- **Dado** un nonce emitido
- **Cuando** el mensaje SIWE omite una línea obligatoria (p. ej. `Version: 1`)
- **Entonces** HTTP 400, `error` = `siwe_parse_failed`
- **Y** `detail` contiene diagnóstico legible (p. ej. línea esperada)

