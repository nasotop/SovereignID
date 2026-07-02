# Issuer service domain contract

El servicio `issuer` concentra la emision, consulta y gobernanza de credenciales verificables del MVP.

## Alcance MVP

- Vincular un titulo emitido a un estudiante mediante la tabla `credentials`.
- Vincular la wallet/DID emisor de una institucion existente.
- Usar la wallet primaria activa del estudiante como sujeto de la credencial.
- Usar el DID emisor de la institucion asociada al estudiante.
- Consultar credenciales del titular autenticado (portal Holder) filtrando por `subject_did` del JWT SIWE.

## Endpoints

| Metodo | Ruta | Proposito |
|--------|------|-----------|
| `POST` | `/issuer/institutions/{institutionId}/wallet` | Vincula la wallet/DID emisor de la institucion |
| `POST` | `/issuer/students/{studentId}/title` | Registra un titulo emitido y lo vincula al estudiante |
| `GET` | `/issuer/holders/me/credentials` | Lista credenciales del titular autenticado (JWT) |
| `GET` | `/issuer/holders/me/credentials/{credentialId}` | Detalle de una credencial del titular autenticado |
| `GET` | `/issuer/credentials/{credentialId}` | Detalle autenticado si la credencial pertenece al titular del JWT |

## Autenticacion (consultas holder)

- Los endpoints `GET` de holder requieren `Authorization: Bearer {jwt}` emitido por el servicio `auth`.
- El filtro de titularidad usa el claim `did` del JWT contra `credentials.subject_did`.
- Las credenciales emitidas a wallets rotadas siguen siendo visibles porque el filtro no depende de la wallet primaria actual.

## Contrato OpenAPI (HTTP)

Snapshot versionado: `docs/contracts/issuer.openapi.json` (regenerar con `bash scripts/export-openapi.sh issuer`; CI valida con `verify-openapi`).

| Elemento | Valor |
|----------|-------|
| Esquema de seguridad | `components.securitySchemes.bearerAuth` — HTTP Bearer, formato JWT |
| Operaciones protegidas en el contrato | `GET /issuer/holders/me/credentials`, `GET /issuer/holders/me/credentials/{credentialId}`, `GET /issuer/credentials/{credentialId}` |
| Operaciones sin Bearer documentado (v1) | `GET /health`, `POST` de emision/vinculacion |
| Campo `status` (holder) | `enum`: `active`, `revoked`, `expired` en `HolderCredentialSummary` y `HolderCredentialDetail` |

El portal web holder consume estos endpoints via cliente Angular generado (`ng-openapi-gen`) y fachada `HolderService`; nginx proxea `/issuer/` hacia `issuer-api`.

## Reglas principales

1. El backend no crea cuentas MetaMask ni wallets.
2. La institucion puede vincular una wallet MetaMask existente como wallet emisora.
3. El estudiante debe existir, estar activo y tener wallet primaria activa.
4. La institucion del estudiante debe existir, estar activa y tener DID emisor.
5. Si se informa una carrera, debe pertenecer a la misma institucion y estar activa.
6. El tipo de credencial debe existir y estar activo.
7. El request de titulo debe incluir CID IPFS, gateway URL, hash de contenido, transaction hash, block number y firma EIP-712.
8. Si `chainId` no viene en el request, se usa `Issuer:DefaultChainId`.
9. Las consultas de solo lectura en Infrastructure usan LINQ con `AsNoTracking`.
10. La API no inyecta `DbContext`; Application usa `ITitleIssuerRepository`, `ICredentialReadStore` y los adapters EF viven en Infrastructure.
11. Issuer valida JWT localmente con la misma clave/`iss`/`aud` que Auth (`Auth:JwtSigningKey`, `Auth:JwtIssuer`, `Auth:JwtAudience`).

## Errores de dominio

| Error | HTTP | Causa |
|-------|------|-------|
| `invalid_institution` | 400 | `institutionId` vacio |
| `invalid_issuer_wallet` | 400 | Wallet/DID emisor incompleto |
| `issuer_wallet_link_failed` | 409 | La institucion no existe o no esta activa |
| `invalid_student` | 400 | `studentId` vacio |
| `invalid_credential_type` | 400 | Tipo de credencial vacio |
| `invalid_title_payload` | 400 | Payload incompleto para emitir/vincular titulo |
| `title_link_failed` | 409 | No existe estudiante, wallet, DID emisor, carrera o tipo de credencial valido |
| `unauthenticated` | 401 | Falta JWT o token invalido |
| `credential_not_found` | 404 | Credencial inexistente o no pertenece al titular autenticado |

## Persistencia

- Modelo EF database-first con efcpt en `Issuer.Infrastructure/Persistence/Generated/`.
- Regenerar: `scripts/scaffold-issuer-db.ps1` (requiere Postgres healthy).
