## 1. Estructura de carpetas

- [x] 1.1 Crear `Persistence/Generated/Entities/`, `Persistence/Stores/ChallengeStore/` y `Persistence/Composition/`
- [x] 1.2 Añadir `.gitkeep` o comentario en README interno solo si alguna carpeta queda vacía antes del movimiento (opcional)

## 2. Zona generada (efcpt)

- [x] 2.1 Actualizar `efcpt-config.json`: `output-path` → `Persistence/Generated`, `model-namespace` → `Persistence.Generated.Entities`
- [x] 2.2 Mover `SovereignIdDbContext.cs` y las 12 entidades EF actuales a `Persistence/Generated/` (o regenerar con efcpt si Postgres está disponible)
- [x] 2.3 Actualizar namespaces a `Auth.Infrastructure.Persistence.Generated` y `Auth.Infrastructure.Persistence.Generated.Entities`
- [x] 2.4 Actualizar `scripts/scaffold-auth-db.ps1` para restaurar `internal` en `Persistence/Generated/SovereignIdDbContext.cs`
- [x] 2.5 Verificar que ningún adapter ni archivo de composición queda bajo `Generated/`

## 3. Adapters IChallengeStore

- [x] 3.1 Mover `InMemoryChallengeStore.cs` de la raíz a `Persistence/Stores/ChallengeStore/`
- [x] 3.2 Mover `PostgresChallengeStore.cs` a `Persistence/Stores/ChallengeStore/`
- [x] 3.3 Unificar namespace `Auth.Infrastructure.Persistence.Stores.ChallengeStore` y visibilidad `internal sealed` en ambos
- [x] 3.4 Actualizar usings: entidad EF desde `Generated.Entities`, DbContext desde `Generated`

## 4. Módulo de composición

- [x] 4.1 Mover `PersistenceOptions.cs` y `PersistenceServiceCollectionExtensions.cs` a `Persistence/Composition/`
- [x] 4.2 Crear `AuthPersistenceServiceCollectionExtensions.cs` con `AddAuthPersistence(IServiceCollection, IConfiguration)`
- [x] 4.3 Refactorizar `DependencyInjection.AddAuthInfrastructure()` para invocar `AddAuthPersistence()` y eliminar lógica duplicada del selector InMemory/Postgres
- [x] 4.4 Actualizar namespaces de composición a `Auth.Infrastructure.Persistence.Composition`

## 5. Documentación

- [x] 5.1 Actualizar mapa de carpetas en `CONTEXT.md` con layout `Generated/`, `Stores/`, `Composition/`
- [x] 5.2 Actualizar glosario: entidades EF en `Persistence/Generated/Entities` (namespace `Generated.Entities`)
- [x] 5.3 Actualizar rutas de referencia en `docs/adr/0002-database-consumption.md` (paths de archivos, sin cambiar decisiones)

## 6. Validación

- [x] 6.1 `dotnet build src/auth/Auth.Infrastructure/Auth.Infrastructure.csproj` sin errores
- [x] 6.2 `dotnet build src/auth/Auth.Api/Auth.Api.csproj` sin errores
- [x] 6.3 `dotnet test tests/auth/Auth.IntegrationTests` — AC-01…AC-07 verdes
- [x] 6.4 Confirmar que no quedan referencias a rutas antiguas (`Persistence/PostgresChallengeStore.cs`, `InMemoryChallengeStore` en raíz) en código fuente
