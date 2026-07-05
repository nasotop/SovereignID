# Authorization domain contract

Este contrato define los roles y permisos transversales del MVP.

## Identidad base

La identidad autenticada se resuelve por SIWE:

- `auth` verifica firma MetaMask y emite JWT.
- El JWT incluye `address`, `did`, `user_id` si existe usuario administrativo, `platform_admin` si aplica, y membresias institucionales.
- Los servicios consumen el JWT con `Authorization: Bearer`.

## Roles globales

Los roles globales viven en `user_global_roles`.

| Rol | Alcance |
|-----|---------|
| `platform_admin` | Usuario maestro de plataforma. Administra tenants/instituciones. |

El primer `platform_admin` se crea por seed SQL con `database/seed-platform-admin.sql`.
Para una BD ya creada, aplicar antes `database/patches/2026-07-04-academy-authz.sql`.

## Roles institucionales

Los roles institucionales viven en `institution_users` y siempre estan acotados a una institucion.

| Rol | Alcance |
|-----|---------|
| `admin` | Superadmin institucional. Gestiona usuarios, carreras, estudiantes y wallets manuales. |
| `issuer` | Operador de emision. Puede asociar wallet emisora y emitir/vincular credenciales. |
| `viewer` | Solo lectura dentro de su institucion. |

El valor historico `student` puede existir en el enum por compatibilidad, pero no se usa para nuevos usuarios institucionales. Los alumnos viven en `students` y `student_wallets`.

## Matriz MVP

| Operacion | `platform_admin` | `admin` | `issuer` | `viewer` |
|-----------|------------------|---------|----------|----------|
| Crear institucion | Si | No | No | No |
| Listar instituciones | Si | No | No | No |
| Ver institucion propia | Si | Si | Si | Si |
| Crear carrera | Si | Si | No | No |
| Crear estudiante | Si | Si | No | No |
| Listar/ver estudiantes | Si | Si | Si | Si |
| Vincular wallet manual a estudiante | Si | Si | No | No |
| Invitar usuarios institucionales | Si | Si | No | No |
| Cambiar/revocar roles institucionales | Si | Si | No | No |
| Asociar wallet/DID emisor | Si | Si | Si | No |
| Vincular titulo emitido | Si | Si | Si | No |
| Ver reportes / dashboard | Si | Si | Si | Si |

## Resolucion de autorizacion

Para el MVP no se crea un microservicio de autorizacion:

- `auth` resuelve roles leyendo `users`, `user_global_roles`, `institution_users` y `student_wallets`.
- `academy` e `issuer` validan policies localmente contra claims del JWT.
- En una etapa posterior se puede mover esta logica a un servicio dedicado si aparece necesidad operacional.

## Seed platform admin

Ejemplo local:

```bash
docker exec -i sovereignid-postgres-local psql -U sovereignid -d sovereignid \
  -v wallet_address='0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa' \
  -v email='admin@sovereignid.local' \
  -v display_name='Platform Admin' \
  -f /path/to/database/seed-platform-admin.sql
```

En Windows, si el archivo esta en el host, se puede montar o ejecutar el contenido con `psql` local si esta instalado.
