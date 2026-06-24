## Why

La capa `Auth.Infrastructure` mezcla en `Persistence/` código generado por efcpt (entidades EF, `SovereignIdDbContext`) con adapters y composición DI escritos a mano. Los dos adapters de `IChallengeStore` viven en carpetas distintas (`InMemoryChallengeStore` en la raíz, `PostgresChallengeStore` en `Persistence/`), y `DependencyInjection.cs` concentra SIWE, casos de uso y selector de persistencia. Esto dificulta la navegación, el scaffold seguro y el cumplimiento de la disciplina de ADR-0002 cuando crezcan más tablas.

## What Changes

- Separar **zona generada** (`Persistence/Generated/`) de **zona escrita** (`Persistence/Stores/`, `Persistence/Composition/`) en `Auth.Infrastructure`.
- Reubicar ambos adapters de `IChallengeStore` bajo `Persistence/Stores/ChallengeStore/` con visibilidad coherente.
- Extraer módulo de composición `AddAuthPersistence()` que encapsule `PersistenceOptions`, registro EF y binding del store.
- Actualizar `efcpt-config.json` y `scripts/scaffold-auth-db.ps1` para la nueva ruta de salida.
- Actualizar `CONTEXT.md` con el layout de carpetas acordado (sin cambiar comportamiento de auth).

## Capabilities

### New Capabilities

- `auth-infrastructure-persistence-layout`: convenciones de organización física de la capa Infrastructure para persistencia EF — zonas generadas vs escrita, ubicación de adapters por seam, y módulo de composición DI.

### Modified Capabilities

- _(ninguna — la reorganización es estructural; los requisitos de `auth-challenge`, SIWE y JWT no cambian)_

## Impact

- **Código**: `src/auth/Auth.Infrastructure/` — movimiento de archivos, namespaces ajustados, nuevo `Persistence/Composition/DependencyInjection.cs` (o equivalente).
- **Tooling**: `efcpt-config.json`, `scripts/scaffold-auth-db.ps1`.
- **Documentación**: `CONTEXT.md` (mapa de carpetas y glosario de entidades EF).
- **Tests**: sin cambios de requisitos; `dotnet test` debe seguir pasando (AC-01…AC-07 con `InMemory`).
- **APIs HTTP / contratos**: sin cambios.
- **ADR-0002**: se refuerza; no se contradice.
