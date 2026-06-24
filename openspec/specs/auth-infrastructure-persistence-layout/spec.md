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

Ambas implementaciones de `IChallengeStore` (`InMemoryChallengeStore` y `PostgresChallengeStore`) SHALL residir bajo `Persistence/Stores/ChallengeStore/`.

Ambas clases MUST ser `internal sealed` y usar el namespace `Auth.Infrastructure.Persistence.Stores.ChallengeStore`.

#### Scenario: Ubicación de adapters de auth challenge

- **WHEN** un desarrollador busca implementaciones de `IChallengeStore` en el repositorio
- **THEN** ambas están en `src/auth/Auth.Infrastructure/Persistence/Stores/ChallengeStore/`
- **AND** ninguna permanece en la raíz de `Auth.Infrastructure`

#### Scenario: Visibilidad coherente

- **WHEN** se compila `Auth.Infrastructure`
- **THEN** `InMemoryChallengeStore` y `PostgresChallengeStore` tienen la misma visibilidad (`internal`)

### Requirement: Módulo de composición de persistencia

La capa Infrastructure SHALL exponer un método de extensión `AddAuthPersistence(IServiceCollection, IConfiguration)` en `Persistence/Composition/` que:

1. Registre `PersistenceOptions` desde configuración.
2. Si `Persistence:Provider` es `Postgres`: registre `SovereignIdDbContext` vía Npgsql y `IChallengeStore` como scoped hacia `PostgresChallengeStore`.
3. Si no: registre `IChallengeStore` como singleton hacia `InMemoryChallengeStore`.

`AddAuthInfrastructure()` MUST delegar el registro de persistencia a `AddAuthPersistence()` sin duplicar la lógica del selector de proveedor.

#### Scenario: Composición desde DependencyInjection raíz

- **WHEN** se inspecciona `DependencyInjection.AddAuthInfrastructure()`
- **THEN** la selección InMemory vs Postgres no aparece inline
- **AND** se invoca `AddAuthPersistence(configuration)`

#### Scenario: Registro InMemory sin Postgres

- **WHEN** `Persistence:Provider` es `InMemory` (o ausente)
- **THEN** `AddAuthPersistence` registra `IChallengeStore` como singleton
- **AND** no registra `SovereignIdDbContext`

#### Scenario: Registro Postgres

- **WHEN** `Persistence:Provider` es `Postgres` y existe connection string
- **THEN** `AddAuthPersistence` registra `SovereignIdDbContext` scoped con Npgsql
- **AND** registra `IChallengeStore` como scoped hacia `PostgresChallengeStore`

### Requirement: Tooling alineado con el layout

`efcpt-config.json` MUST configurar `file-layout.output-path` como `Persistence/Generated`.

`scripts/scaffold-auth-db.ps1` MUST restaurar `internal` en `Persistence/Generated/SovereignIdDbContext.cs` tras regenerar.

#### Scenario: Config efcpt actualizada

- **WHEN** se lee `efcpt-config.json` del proyecto
- **THEN** `output-path` apunta a `Persistence/Generated`
- **AND** `model-namespace` refleja `Persistence.Generated.Entities`

### Requirement: Sin cambio de comportamiento funcional

La reorganización MUST NOT alterar el comportamiento observable de `GET /auth/nonce`, `POST /auth/verify`, ni los criterios de aceptación AC-01…AC-07.

Los tests de integración existentes MUST pasar sin modificar sus aserciones de dominio HTTP.

#### Scenario: Tests de integración verdes

- **WHEN** se ejecuta `dotnet test tests/auth/Auth.IntegrationTests` tras la reorganización
- **THEN** todos los tests pasan con la misma configuración `Persistence:Provider=InMemory` que antes
