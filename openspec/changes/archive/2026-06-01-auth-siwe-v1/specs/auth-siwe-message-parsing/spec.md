## ADDED Requirements

### Requirement: Parse EIP-4361 payload line by line

The service SHALL parse the SIWE `message` string line by line, normalizing `\r\n` to `\n`.

The payload MUST contain at least 10 lines in fixed order:

1. `{domain} wants you to sign in with your Ethereum account:`
2. `{0xAddress}` — `0x` plus 40 hex digits
3. empty line
4. `{statement}` (line may be empty)
5. empty line
6. `URI: {absolute uri}`
7. `Version: 1` (literal)
8. `Chain ID: {decimal}`
9. `Nonce: {32 lowercase hex}`
10. `Issued At: {ISO8601}`

Optional fields after line 10 MAY include `Expiration Time`, `Not Before`, `Request ID`, and `Resources:` block per EIP-4361 grammar.

Any other non-empty unrecognized line MUST cause parse failure.

The parser MUST retain the original message bytes/string as `OriginalPayload` without re-serialization for signature verification.

#### Scenario: Malformed payload missing required line (AC-07)

- **WHEN** a nonce was issued
- **WHEN** verify message omits a mandatory line (e.g. `Version: 1`)
- **THEN** HTTP 400 with `error` equal to `siwe_parse_failed`
- **AND** `detail` contains human-readable diagnostic (e.g. expected line)

### Requirement: Parse-time field validation

Invalid address on line 2 or `Version` not equal to `1` MUST yield HTTP 400 with `error` equal to `siwe_parse_failed`.

#### Scenario: Invalid version in message

- **WHEN** message contains `Version: 2`
- **THEN** HTTP 400 and `error` is `siwe_parse_failed`
