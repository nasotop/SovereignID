## ADDED Requirements

### Requirement: Modelo EF propio y acotado del verifier

El ensamblado `Verifier.Infrastructure` SHALL tener su propio modelo EF generado database-first, independiente del de `auth`. El modelo generado MUST NOT compartirse ni referenciarse desde otro microservicio.

El scaffold SHALL acotarse exactamente a las tablas: `credentials`, `verification_logs`, `credential_types`, `institutions`. MUST NOT generar entidades para otras tablas del esquema.

Todo el código generado SHALL residir bajo `Persistence/Generated/`; el `DbContext` MUST permanecer `internal` y MUST NOT inyectarse en `Verifier.Api` ni en los casos de uso.

#### Scenario: Alcance del scaffold

- **WHEN** se inspecciona `Verifier.Infrastructure/Persistence/Generated/Entities`
- **THEN** solo existen entidades para `credentials`, `verification_logs`, `credential_types` e `institutions`

#### Scenario: DbContext aislado del de auth

- **WHEN** se inspecciona el modelo generado del verifier
- **THEN** define su propio `DbContext` `internal` y no referencia el ensamblado de `auth`

### Requirement: Namespaces del modelo generado del verifier

Las entidades EF generadas del verifier MUST usar el namespace `Verifier.Infrastructure.Persistence.Generated.Entities`.

El `DbContext` generado MUST usar el namespace `Verifier.Infrastructure.Persistence.Generated`.

#### Scenario: Namespace de entidad scaffold del verifier

- **WHEN** se inspecciona una entidad EF como `Credential` en la zona generada del verifier
- **THEN** su namespace es `Verifier.Infrastructure.Persistence.Generated.Entities`

### Requirement: Persistencia del verifier Postgres-only vía interfaces de Application

`Verifier.Application` SHALL definir las interfaces `ICredentialReadStore` (lectura de credencial con su tipo y emisor) e `IVerificationLogStore` (escritura del intento de verificación).

`Verifier.Infrastructure` SHALL proveer exactamente un adapter EF por interfaz: `EfCredentialReadStore` e `EfVerificationLogStore`, ambos `internal sealed`, bajo `Persistence/Stores/`. MUST NOT existir un adapter InMemory ni un toggle de proveedor.

La composición DI SHALL registrar el `DbContext` vía Npgsql y los adapters EF; MUST exigir `ConnectionStrings:DefaultConnection`.

#### Scenario: Registro de persistencia del verifier

- **WHEN** se inspecciona la composición DI de `Verifier.Infrastructure`
- **THEN** registra `VerifierDbContext` vía Npgsql, `ICredentialReadStore` hacia `EfCredentialReadStore` e `IVerificationLogStore` hacia `EfVerificationLogStore`
- **AND** no existe ninguna rama de selección InMemory vs Postgres

#### Scenario: Connection string obligatoria

- **WHEN** se arranca el verifier sin `ConnectionStrings:DefaultConnection`
- **THEN** la validación de configuración falla con un error claro

### Requirement: Tests de integración del verifier con Postgres efímero

Los tests de integración del verifier SHALL ejecutarse contra un PostgreSQL efímero (Testcontainers), aplicando el esquema canónico `database/BBDD_SovereignID.sql` y sembrando datos de prueba. MUST NOT depender de un store InMemory.

#### Scenario: Suite de integración del verifier

- **WHEN** se ejecuta la suite de integración del verifier
- **THEN** se levanta un contenedor PostgreSQL, se aplica el esquema canónico y se ejercen los escenarios de `POST /verifications` de punta a punta
