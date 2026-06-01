# auth-jwt-session Specification

## Purpose
TBD - created by archiving change auth-siwe-v1. Update Purpose after archive.
## Requirements
### Requirement: Issue session JWT after successful verify

After successful SIWE verification the service MUST consume the nonce and issue a JWT with session TTL of 24 hours.

The verify response `expiresAt` MUST reflect JWT expiration in UTC ISO 8601.

#### Scenario: JWT issued on successful verify

- **WHEN** verify completes successfully
- **THEN** response includes non-empty `jwt` and `expiresAt` approximately 24 hours after issuance

### Requirement: JWT signing algorithm and key

The JWT MUST be signed with HMAC-SHA256 (`HS256`) using symmetric key from configuration/environment variable `AUTH_JWT_SIGNING_KEY`.

Outside Development environment the signing key MUST be present and at least 32 UTF-8 bytes; startup MUST fail if missing or too short.

Issuer (`iss`) and audience (`aud`) MUST be configurable (defaults acceptable for this monorepo).

#### Scenario: Missing signing key in production

- **WHEN** application runs outside Development without `AUTH_JWT_SIGNING_KEY` of sufficient length
- **THEN** application fails to start

### Requirement: Required JWT claims

The JWT MUST include claims:

| Claim | Content |
|-------|---------|
| `sub` | Ethereum address lowercase (`0x` + 40 hex) |
| `address` | Same value as `sub` |
| `did` | `did:ethr:sepolia:{sub}` string derivation without on-chain resolution |
| `iat` | Issued at (Unix seconds) |
| `exp` | Expiration 24h after `iat` |
| `iss` | Configured issuer |
| `aud` | Configured audience |

#### Scenario: sub claim matches signer lowercase (AC-01)

- **WHEN** verify succeeds for a wallet address
- **THEN** decoded JWT `sub` equals that address in lowercase

