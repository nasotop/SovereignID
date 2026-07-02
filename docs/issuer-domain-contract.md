# Issuer service domain contract

El servicio `issuer` concentra la emision y vinculacion de credenciales verificables del MVP.

## Alcance MVP

- Vincular un titulo emitido a un estudiante mediante la tabla `credentials`.
- Vincular la wallet/DID emisor de una institucion existente.
- Usar la wallet primaria activa del estudiante como sujeto de la credencial.
- Usar el DID emisor de la institucion asociada al estudiante.

## Endpoint

| Metodo | Ruta | Proposito |
|--------|------|-----------|
| `POST` | `/issuer/institutions/{institutionId}/wallet` | Vincula la wallet/DID emisor de la institucion |
| `POST` | `/issuer/students/{studentId}/title` | Registra un titulo emitido y lo vincula al estudiante |
| `GET` | `/issuer/institutions/{institutionId}/credentials` | Lista credenciales emitidas por la institucion |
| `GET` | `/issuer/credentials/{credentialId}` | Consulta una credencial |
| `POST` | `/issuer/credentials/{credentialId}/revoke` | Revoca una credencial activa tras tx on-chain |

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
10. La API no inyecta `DbContext`; Application usa `ITitleIssuerRepository` y el adapter EF vive en Infrastructure.

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
| `credential_not_found` | 404 | Credencial inexistente |
| `credential_not_active` | 409 | Solo credenciales activas pueden revocarse |
| `invalid_revocation_reason` | 400 | Motivo de revocacion vacio |
| `invalid_revocation_payload` | 400 | Evidencia de revocacion incompleta |
| `blockchain_anchor_invalid` | 409 | La transaccion on-chain no pudo verificarse |
