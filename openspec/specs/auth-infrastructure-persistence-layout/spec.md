# auth-infrastructure-persistence-layout Specification

## Purpose

Convenciones de organización física de la capa `Auth.Infrastructure` para persistencia EF: separación entre código generado y escrito, colocación de adapters por seam, y módulo de composición DI. Refuerza ADR-0002 sin cambiar comportamiento de auth.

## Requirements

### Requirement: Zona generada aislada bajo Persistence/Generated

El ensamblado `Auth.Infrastructure` SHALL colocar todo el código producido por efcpt exclusivamente bajo `Persistence/Generated/`, incluyendo `SovereignIdDbContext` y las entidades EF del esquema scaffold.

Ningún adapter escrito a mano, opción de configuración ni extensión DI SHALL residir en `Persistence/Generated/`.

Tras cada regeneración, `SovereignIdDbContext` MUST permanecer `internal` al ensamblado.

#### Scenario: Regeneración del modelo EF

- **WHEN** un desarrollador ejecuta `scripts/scaffold-auth-db.ps1` con Postgres healthy
- **THEN** efcpt escribe entidades y DbContext bajo `Persistence/Generated/`
- **AND** archivos existentes en `Persistence/Stores/` y `Persistence/Composition/` no son sobrescritos

#### Scenario: Navegación del código generado

- **WHEN** un contribuidor abre `Persistence/Generated/`
- **THEN** solo encuentra código scaffold (DbContext y entidades EF)
- **AND** no encuentra adapters ni registros DI

### Requirement: Namespaces alineados con el layout generado

Las entidades EF generadas MUST usar el namespace `Auth.Infrastructure.Persistence.Generated.Entities`.

`SovereignIdDbContext` MUST usar el namespace `Auth.Infrastructure.Persistence.Generated`.

#### Scenario: Namespace de entidad scaffold

- **WHEN** se inspecciona una entidad EF como `AuthChallenge` en la zona generada
- **THEN** su namespace es `Auth.Infrastructure.Persistence.Generated.Entities`

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

### Requirement: Tooling alineado con el layout

`efcpt-config.json` MUST configurar `file-layout.output-path` como `Persistence/Generated`.

`scripts/scaffold-auth-db.ps1` MUST restaurar `internal` en `Persistence/Generated/SovereignIdDbContext.cs` tras regenerar.

#### Scenario: Config efcpt actualizada

- **WHEN** se lee `efcpt-config.json` del proyecto
- **THEN** `output-path` apunta a `Persistence/Generated`
- **AND** `model-namespace` refleja `Persistence.Generated.Entities`

### Requirement: Sin cambio de comportamiento funcional

La reorganización MUST NOT alterar el comportamiento observable de `GET /auth/nonce`, `POST /auth/verify`, ni los criterios de aceptación AC-01…AC-07.

Los tests de integración MUST seguir validando AC-01…AC-07 sin cambiar sus aserciones de dominio HTTP, ejecutándose ahora contra un PostgreSQL efímero (Testcontainers) en lugar del proveedor InMemory.

#### Scenario: Tests de integración verdes sobre Postgres

- **WHEN** se ejecuta `dotnet test tests/auth/Auth.IntegrationTests` tras el cambio
- **THEN** todos los tests AC-01…AC-07 pasan contra un PostgreSQL efímero (Testcontainers)
- **AND** las aserciones de dominio HTTP permanecen sin cambios
