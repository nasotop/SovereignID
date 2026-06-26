## MODIFIED Requirements

### Requirement: Adapters de IChallengeStore colocados juntos

La única implementación de `IChallengeStore` (`EfChallengeStore`) SHALL residir bajo `Persistence/Stores/ChallengeStore/`.

La clase MUST ser `internal sealed` y usar el namespace `Auth.Infrastructure.Persistence.Stores.ChallengeStore`.

No SHALL existir ningún adapter `InMemoryChallengeStore` ni ninguna otra implementación alternativa de `IChallengeStore`.

#### Scenario: Ubicación del adapter de auth challenge

- **WHEN** un desarrollador busca implementaciones de `IChallengeStore` en el repositorio
- **THEN** encuentra exactamente una, `EfChallengeStore`, en `src/auth/Auth.Infrastructure/Persistence/Stores/ChallengeStore/`
- **AND** no existe ningún `InMemoryChallengeStore`

#### Scenario: Visibilidad del adapter

- **WHEN** se compila `Auth.Infrastructure`
- **THEN** `EfChallengeStore` es `internal sealed`

### Requirement: Módulo de composición de persistencia

La capa Infrastructure SHALL exponer un método de extensión `AddAuthPersistence(IServiceCollection, IConfiguration)` en `Persistence/Composition/` que:

1. Registre `SovereignIdDbContext` vía Npgsql usando `ConnectionStrings:DefaultConnection`.
2. Registre `IChallengeStore` como scoped hacia `EfChallengeStore`.

No SHALL existir un selector de proveedor (`Persistence:Provider`) ni una rama de registro alternativa. `ConnectionStrings:DefaultConnection` MUST ser obligatoria.

`AddAuthInfrastructure()` MUST delegar el registro de persistencia a `AddAuthPersistence()` sin duplicar lógica.

#### Scenario: Composición desde DependencyInjection raíz

- **WHEN** se inspecciona `DependencyInjection.AddAuthInfrastructure()`
- **THEN** invoca `AddAuthPersistence(configuration)` sin lógica de selección de proveedor inline

#### Scenario: Registro único respaldado por EF

- **WHEN** se inspecciona `AddAuthPersistence`
- **THEN** registra `SovereignIdDbContext` scoped con Npgsql
- **AND** registra `IChallengeStore` como scoped hacia `EfChallengeStore`
- **AND** no contiene ninguna rama InMemory

#### Scenario: Connection string obligatoria

- **WHEN** se arranca `auth` sin `ConnectionStrings:DefaultConnection`
- **THEN** la validación de configuración falla con un error claro

### Requirement: Sin cambio de comportamiento funcional

La reorganización MUST NOT alterar el comportamiento observable de `GET /auth/nonce`, `POST /auth/verify`, ni los criterios de aceptación AC-01…AC-07.

Los tests de integración MUST seguir validando AC-01…AC-07 sin cambiar sus aserciones de dominio HTTP, ejecutándose ahora contra un PostgreSQL efímero (Testcontainers) en lugar del proveedor InMemory.

#### Scenario: Tests de integración verdes sobre Postgres

- **WHEN** se ejecuta `dotnet test tests/auth/Auth.IntegrationTests` tras el cambio
- **THEN** todos los tests AC-01…AC-07 pasan contra un PostgreSQL efímero (Testcontainers)
- **AND** las aserciones de dominio HTTP permanecen sin cambios
