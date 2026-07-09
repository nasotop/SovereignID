# ADR-0006: Consolidar dominio identitario — sin microservicio `identity` en MVP

## Estado

Aceptado — 2026-07-05

## Contexto

El documento inicial del proyecto (`contexto_proyecto.md`) asignaba a `Identity.API` el CRUD de instituciones y alumnos. Durante la implementación greenfield, ese alcance se implementó en **`academy`**, la resolución criptográfica en **`auth`**, y las credenciales del titular en **`issuer`**. Quedó un scaffold `identity-api` (solo `GET /health`) desplegado en Docker y registrado en el BFF, sin endpoints de negocio ni consumidores.

Eso generaba fricción arquitectónica: un servicio desplegable sin interfaz de profundidad, mientras el dominio identitario real estaba repartido en tres módulos con comportamiento ya operativo.

Alternativas evaluadas:

1. **Activar `identity-api`** moviendo perfil holder y/o entidades transversales desde academy.
2. **Consolidar y eliminar** el scaffold; documentar el reparto v1 explícitamente.
3. **Revertir** academy → identity (CRUD centralizado).

Para cerrar el MVP, la prioridad es completar funcionalidad existente, no migrar código entre servicios.

## Decisión

**No hay microservicio `identity` en el MVP v1.** Se elimina `src/identity/`, sus tests, contrato OpenAPI, cliente Kiota downstream y contenedor Docker.

### Reparto identitario v1

| Concern | Módulo dueño |
|---------|--------------|
| Autenticación SIWE y emisión JWT | `auth` |
| Resolución de roles en login (`platform_admin`, membresías, claim `holder`) | `auth` (lee `users`, `user_global_roles`, `institution_users`, `student_wallets`) |
| Tenants: instituciones, carreras, estudiantes, invitaciones, usuarios institucionales | `academy` |
| Perfil off-chain del titular (`GET/PUT /academy/holders/me`) | `academy` |
| Credenciales verificables del titular | `issuer` |
| Autorización downstream (políticas JWT) | `SovereignID.Authorization` en cada servicio consumidor |

### Reglas de modelo conservadas

- Una misma wallet **puede** ser holder (vía `student_wallets`) y usuario institucional (vía `users` + `institution_users`) simultáneamente.
- La tabla `users` sirve usuarios institucionales y, de forma lazy, filas creadas al editar perfil holder.
- El claim `holder` en JWT se resuelve desde `student_wallets` primaria activa, no desde `holder_profiles`.

### Infraestructura

- `docker-compose.yml`: sin servicio `identity-api`.
- BFF downstream v1: `verifier`, `issuer`, `academy`, `reports` (sin `identity`).
- OpenAPI registry (`scripts/openapi-lib.sh`): sin entrada `identity`.

## Consecuencias

### Positivas

- El mapa de despliegue coincide con el mapa de dominio; menos contenedores y menos confusión para exploradores del repo.
- El esfuerzo de MVP se concentra en completar CRUD y reglas en `academy`, no en activar un servicio vacío.
- ADR-0005 (BFF) queda alineado con los downstream reales.

### Negativas

- Si en v2 se necesita un servicio de identidad transversal (p. ej. registro DID centralizado), habrá que crear un módulo nuevo o reintroducir `identity` con interfaz real — no reutilizar el scaffold eliminado.
- Quien lea `contexto_proyecto.md` debe consultar este ADR y `CONTEXT.md` para el reparto actual.

## Referencias

- [ADR-0005](0005-bff-kiota.md) — BFF y downstream (actualizar tabla de servicios)
- [`CONTEXT.md`](../../CONTEXT.md) — sección «Reparto identitario v1»
- [`docs/academy-domain-contract.md`](../academy-domain-contract.md)
- [`docs/authorization-domain-contract.md`](../authorization-domain-contract.md)
