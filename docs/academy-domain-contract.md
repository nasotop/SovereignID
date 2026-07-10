# Academy service domain contract

El servicio `academy` concentra el alcance academico del MVP:

- Crear instituciones como tenants.
- Invitar usuarios institucionales por email para que vinculen una wallet MetaMask existente.
- Administrar usuarios institucionales dentro del tenant.
- Crear carreras por institucion.
- Mantener el pool de carreras de una institucion: listar, consultar, editar y desactivar carreras.
- Crear estudiantes por institucion y, opcionalmente, vincular una wallet existente del estudiante.
- Vincular manualmente una wallet existente a un estudiante.
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
9. `platform_admin` crea instituciones; `admin` gestiona su institucion; `viewer` solo lee.
10. El rol historico `student` no se usa para nuevas invitaciones institucionales.

## Endpoints

Todos los endpoints mutables requieren `Authorization: Bearer {jwt}` emitido por `auth`, salvo `POST /academy/invitations/accept` (token de invitacion).

| Endpoint | Politica | Proposito |
|----------|----------|-----------|
| `POST /academy/institutions` | `PlatformAdmin` | Crea institucion y genera invitacion admin |
| `GET /academy/institutions` | `PlatformAdmin` | Lista instituciones para administracion de plataforma |
| `GET /academy/institutions/{institutionId}` | `PlatformOrInstitutionMember` | Consulta institucion |
| `POST /academy/institutions/{institutionId}/careers` | `InstitutionAdmin` | Crea carrera |
| `GET /academy/institutions/{institutionId}/careers` | `PlatformOrInstitutionMember` | Lista carreras de la institucion |
| `GET /academy/institutions/{institutionId}/careers/{careerId}` | `PlatformOrInstitutionMember` | Consulta carrera |
| `PATCH /academy/institutions/{institutionId}/careers/{careerId}` | `InstitutionAdmin` | Actualiza codigo y nombre de carrera |
| `DELETE /academy/institutions/{institutionId}/careers/{careerId}` | `InstitutionAdmin` | Desactiva carrera sin borrar historial |
| `POST /academy/institutions/{institutionId}/students` | `InstitutionAdmin` | Crea estudiante, con wallet opcional |
| `GET /academy/institutions/{institutionId}/students` | `PlatformOrInstitutionMember` | Lista estudiantes de la institucion |
| `GET /academy/institutions/{institutionId}/students/{studentId}` | `PlatformOrInstitutionMember` | Consulta estudiante |
| `POST /academy/institutions/{institutionId}/students/{studentId}/wallets` | `InstitutionAdmin` | Vincula wallet manual a estudiante |
| `POST /academy/institutions/{institutionId}/invitations` | `InstitutionAdmin` | Invita otro usuario institucional |
| `POST /academy/institutions/{institutionId}/users/invitations` | `InstitutionAdmin` | Alias para invitar usuario institucional |
| `GET /academy/institutions/{institutionId}/users` | `InstitutionAdmin` | Lista usuarios institucionales |
| `PATCH /academy/institutions/{institutionId}/users/{userId}/role` | `InstitutionAdmin` | Cambia rol institucional |
| `DELETE /academy/institutions/{institutionId}/users/{userId}` | `InstitutionAdmin` | Revoca acceso institucional |
| `POST /academy/invitations/accept` | Publico | Acepta invitacion y vincula wallet MetaMask existente |

## Errores

Los errores de negocio usan RFC 7807 Problem Details con extension `error`, siguiendo ADR-0001.

Codigos principales:

| Codigo | HTTP | Caso |
|--------|------|------|
| (sin JWT / token invalido) | 401 | Falta autenticacion en endpoints protegidos |
| (sin permiso) | 403 | JWT valido pero sin rol requerido |
| `invalid_institution` | 400 | Faltan campos obligatorios de institucion |
| `invalid_invitation_email` | 400 | Email de invitacion invalido |
| `institution_code_exists` | 409 | Codigo de institucion duplicado |
| `institution_not_found` | 404 | Institucion inexistente |
| `invalid_career` | 400 | Carrera sin codigo o nombre |
| `career_code_exists` | 409 | Codigo de carrera duplicado en la institucion |
| `career_not_found` | 404 | Carrera inexistente para la institucion |
| `student_external_reference_exists` | 409 | Referencia externa duplicada en la institucion |
| `student_not_found` | 404 | Estudiante inexistente para la institucion |
| `invalid_wallet_address` | 400 | Wallet no tiene formato Ethereum `0x` + 40 hex |
| `invalid_institution_role` | 400 | Rol no soportado |
| `institution_user_not_found` | 404 | Usuario institucional inexistente o revocado |
| `invalid_invitation_token` | 400 | Token faltante |
| `invitation_not_usable` | 404 | Token inexistente, expirado o ya aceptado |

## Autorizacion

Detalle transversal: [`docs/authorization-domain-contract.md`](authorization-domain-contract.md).

