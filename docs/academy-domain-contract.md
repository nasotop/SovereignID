# Academy service domain contract

El servicio `academy` concentra el alcance academico del MVP:

- Crear instituciones como tenants.
- Invitar usuarios institucionales por email para que vinculen una wallet MetaMask existente.
- Crear carreras por institucion.
- Crear estudiantes por institucion y, opcionalmente, vincular una wallet existente del estudiante.
- La wallet/DID emisor de la institucion se vincula en el servicio `issuer`.

## Reglas principales

1. El backend no crea cuentas MetaMask ni wallets.
2. El link de invitacion contiene un token temporal; en BD se guarda solo el hash SHA-256 del token.
3. Al aceptar la invitacion, el usuario conecta su wallet en el frontend y el backend guarda `wallet_address` y `did`.
4. El link no se puede reutilizar despues de aceptado o expirado.
5. La institucion puede crearse sin wallet/DID emisor; esa vinculacion pertenece al servicio `issuer`.
6. La emision o vinculacion de titulos no pertenece a `academy`; la coordina el servicio `issuer`.
7. Las consultas de solo lectura en Infrastructure usan LINQ con `AsNoTracking`.
8. La API no inyecta `DbContext`; Application usa `IAcademyRepository` y el adapter EF vive en Infrastructure.

## Endpoints

| Endpoint | Proposito |
|----------|-----------|
| `POST /academy/institutions` | Crea institucion y genera invitacion admin |
| `GET /academy/institutions/{institutionId}` | Consulta institucion |
| `POST /academy/institutions/{institutionId}/careers` | Crea carrera |
| `POST /academy/institutions/{institutionId}/students` | Crea estudiante, con wallet opcional |
| `POST /academy/institutions/{institutionId}/invitations` | Invita otro usuario institucional |
| `POST /academy/invitations/accept` | Acepta invitacion y vincula wallet MetaMask existente |

## Errores

Los errores de negocio usan RFC 7807 Problem Details con extension `error`, siguiendo ADR-0001.

Codigos principales:

| Codigo | HTTP | Caso |
|--------|------|------|
| `invalid_institution` | 400 | Faltan campos obligatorios de institucion |
| `invalid_invitation_email` | 400 | Email de invitacion invalido |
| `institution_code_exists` | 409 | Codigo de institucion duplicado |
| `institution_not_found` | 404 | Institucion inexistente |
| `invalid_career` | 400 | Carrera sin codigo o nombre |
| `career_code_exists` | 409 | Codigo de carrera duplicado en la institucion |
| `student_external_reference_exists` | 409 | Referencia externa duplicada en la institucion |
| `invalid_wallet_address` | 400 | Wallet no tiene formato Ethereum `0x` + 40 hex |
| `invalid_institution_role` | 400 | Rol no soportado |
| `invalid_invitation_token` | 400 | Token faltante |
| `invitation_not_usable` | 404 | Token inexistente, expirado o ya aceptado |

