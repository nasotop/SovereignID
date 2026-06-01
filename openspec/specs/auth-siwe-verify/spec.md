# auth-siwe-verify Specification

## Purpose
TBD - created by archiving change auth-siwe-v1. Update Purpose after archive.
## Requirements
### Requirement: POST /auth/verify contract

The service SHALL expose `POST /auth/verify` accepting JSON body with `message` (full multiline SIWE payload as signed) and `signature` (hex with `0x` prefix from `personal_sign`).

On success MUST return HTTP 200 with `jwt`, `address`, and `expiresAt` (JWT expiry, UTC ISO 8601).

#### Scenario: Happy path (AC-01)

- **WHEN** client obtains nonce, signs valid Sepolia SIWE message with correct nonce, and posts verify
- **THEN** HTTP 200 with `jwt`, `address`, and `expiresAt`
- **AND** JWT claim `sub` equals signer address in lowercase

### Requirement: Validation order for verify pipeline

The service MUST evaluate failures in this order after receiving verify:

1. SIWE parse failures → `siwe_parse_failed` (400)
2. Nonce not issued → `nonce_unknown` (401)
3. Nonce expired → `nonce_expired` (401)
4. Nonce already consumed → `nonce_consumed` (401)
5. Chain ID not Sepolia → `unsupported_chain` (400)
6. Signature recovery mismatch → `signature_mismatch` (401)

Chain ID check MUST occur only after establishing the nonce exists in the store (not before unknown-nonce classification).

#### Scenario: Unsupported chain after known nonce (AC-04)

- **WHEN** a nonce was issued
- **WHEN** message declares `Chain ID: 1` (or any value other than `11155111`) with otherwise coherent signature and nonce
- **THEN** HTTP 400 and `error` is `unsupported_chain`

### Requirement: Sepolia-only chain policy

The service MUST accept only Ethereum Sepolia chain ID `11155111` in the SIWE message.

#### Scenario: Sepolia chain ID accepted

- **WHEN** a nonce was issued
- **WHEN** message declares `Chain ID: 11155111` with otherwise valid SIWE fields
- **THEN** verify proceeds past chain policy (signature and nonce checks may still fail independently)

### Requirement: Signature verification on original payload

The service MUST recover the signer address from `personal_sign` / EIP-191 over the UTF-8 of `OriginalPayload`.

Recovered address MUST be compared to the address in the SIWE message case-insensitively (EIP-55 normalization recommended).

Mismatch or signature valid for a different payload MUST return HTTP 401 with `error` equal to `signature_mismatch`.

#### Scenario: Tampered message (AC-05)

- **WHEN** nonce issued and signature valid for original message
- **WHEN** verify sends modified message with original signature
- **THEN** HTTP 401 and `error` is `signature_mismatch`

### Requirement: Authentication error envelope

Failed auth responses MUST use RFC 7807 Problem Details with fields: `title` = `"Authentication failed"`, `status` (400 or 401), `detail` (human-readable), and `error` (stable snake_case code).

Supported `error` codes: `siwe_parse_failed`, `unsupported_chain`, `nonce_unknown`, `nonce_expired`, `nonce_consumed`, `signature_mismatch`.

#### Scenario: Parse error uses stable code

- **WHEN** verify receives malformed SIWE
- **THEN** Problem Details include `error` field with stable snake_case value

