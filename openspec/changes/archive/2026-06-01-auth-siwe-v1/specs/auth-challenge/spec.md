## ADDED Requirements

### Requirement: Emit auth challenge via GET /auth/nonce

The auth service SHALL expose `GET /auth/nonce` returning HTTP 200 with JSON body containing `nonce` and `expiresAt`.

The `nonce` MUST be exactly 32 lowercase hexadecimal characters matching `[0-9a-f]{32}` without `0x` prefix, with 128 bits of entropy.

The `expiresAt` MUST be an instant in UTC expressed as ISO 8601 / RFC 3339 (e.g. suffix `Z`).

Each issued nonce MUST be unique among active challenges in the store.

#### Scenario: Successful nonce issuance

- **WHEN** client calls `GET /auth/nonce`
- **THEN** response status is 200
- **AND** body contains `nonce` matching `[0-9a-f]{32}` and `expiresAt` in UTC ISO 8601

### Requirement: Auth challenge TTL and persistence

The service SHALL persist each issued challenge until it expires or is successfully consumed.

The default challenge TTL MUST be 600 seconds from issuance.

#### Scenario: Challenge expires after TTL (AC-03)

- **WHEN** a nonce was issued and more than 600 seconds elapse before verify
- **THEN** `POST /auth/verify` returns HTTP 401 with `error` equal to `nonce_expired`

### Requirement: Single-use consumption

A valid challenge MUST be consumable at most once by a successful verify.

After successful verify, a repeat request with the same valid `(message, signature)` MUST return HTTP 401 with `error` equal to `nonce_consumed`.

#### Scenario: Replay after successful login (AC-02)

- **WHEN** verify succeeded for a given `(message, signature)`
- **WHEN** the same `POST /auth/verify` is sent again
- **THEN** HTTP 401 and `error` is `nonce_consumed`

### Requirement: Unknown nonce

If the SIWE message references a nonce never issued by this backend, verify MUST return HTTP 401 with `error` equal to `nonce_unknown`.

#### Scenario: Wrong nonce in message (AC-06)

- **WHEN** nonce `N` was issued
- **WHEN** verify message contains nonce `N'` never issued
- **THEN** HTTP 401 and `error` is `nonce_unknown`
