# ADR-0003: Persistencia única respaldada por EF (retiro de InMemory)

## Estado

Aceptado — 2026-06-25

**Supersede** a [ADR-0002](0002-database-consumption.md) **en el punto del proveedor de persistencia** (toggle `Persistence:Provider` y adapter `InMemory`). El resto de ADR-0002 (database-first, seams Application↔Infrastructure, `DbContext` internal, reglas de escritura) sigue vigente.

## Contexto

ADR-0002 definió dos proveedores de persistencia seleccionables vía `Persistence:Provider`:

- `InMemory` (default): adapters en memoria, sin Postgres al arranque.
- `Postgres`: adapters EF sobre la BD canónica.

El proveedor `InMemory` nació como **artefacto de demo**: permitía ejecutar `auth` y sus tests AC-01…AC-07 sin levantar Postgres. Tiene costes:

1. **No durable ni multi-instancia**: los retos viven en el proceso; inservible en producción real.
2. **Doble camino de código**: cada interfaz de Application necesita dos adapters y una rama de selección, que divergen con el tiempo.
3. **Tests que no ejercen el adapter real**: AC-01…AC-07 pasaban contra memoria, no contra el SQL/EF que corre en producción.
4. **Confusión de configuración**: `Persistence:Provider` + connection string condicional.

Con la BD canónica disponible y Testcontainers como forma estándar de levantar Postgres efímero, el proveedor `InMemory` deja de aportar valor.

## Decisión

El monorepo tiene **una única persistencia, respaldada por EF Core (Npgsql) sobre la BD canónica**. Se retira el proveedor `InMemory`.

| Aspecto | Antes (ADR-0002) | Ahora (ADR-0003) |
|---------|------------------|------------------|
| Proveedores | `InMemory` \| `Postgres` | EF/Npgsql único |
| Toggle `Persistence:Provider` | sí (default `InMemory`) | **eliminado** |
| `ConnectionStrings:DefaultConnection` | obligatoria solo si `Postgres` | **siempre obligatoria** |
| Adapter de `IChallengeStore` (auth) | `InMemoryChallengeStore` \| `PostgresChallengeStore` | `EfChallengeStore` (único) |
| Tests de integración | `InMemory` por defecto | Postgres efímero (Testcontainers) |

### Cambios concretos

- **auth:** se elimina `InMemoryChallengeStore`. `PostgresChallengeStore` → `EfChallengeStore` (clase y archivo; namespace `Auth.Infrastructure.Persistence.Stores.ChallengeStore` intacto). `AddAuthPersistence` registra siempre `SovereignIdDbContext` (Npgsql) + `IChallengeStore` → `EfChallengeStore`. Se eliminan `PersistenceOptions`/`PersistenceProviders`/`UsesPostgresPersistence` y la rama de selección.
- **verifier:** persistencia Postgres-only desde su primera implementación; sin `PersistenceOptions` ni toggle.
- **Validación de arranque:** `ValidateAuthConfiguration` / `ValidateVerifierConfiguration` fallan con error claro si falta `ConnectionStrings:DefaultConnection`.
- **Tests:** `Auth.IntegrationTests` y `Verifier.IntegrationTests` levantan Postgres efímero con Testcontainers, aplican `database/BBDD_SovereignID.sql` y ejercen los endpoints de punta a punta. Las aserciones de dominio HTTP de AC-01…AC-07 no cambian.

## Consecuencias

### Positivas

- Un solo camino de persistencia: menos código, menos divergencia, menos sorpresas entre dev y prod.
- Los tests ejercen el adapter EF real contra Postgres, ganando fidelidad.
- Configuración más simple: una connection string obligatoria, sin selector.

### Negativas

- Los tests de integración ahora **requieren Docker** (Testcontainers); CI debe disponer de un daemon de contenedores.
- Ejecutar los servicios localmente exige una connection string válida (no hay modo "sin BD").
- Reversión más costosa que un toggle: restaurar `InMemory` implicaría reintroducir adapter + rama de selección.

## Referencias

- [ADR-0002](0002-database-consumption.md) — superseded en el punto del proveedor; resto vigente
- [ADR-0001](0001-problem-details-errors.md) — errores HTTP (independiente de persistencia)
- [`CONTEXT.md`](../../CONTEXT.md) — mapa del monorepo y glosario
- `src/auth/Auth.Infrastructure/Persistence/Stores/ChallengeStore/EfChallengeStore.cs` — adapter único de auth
- `database/BBDD_SovereignID.sql` — esquema canónico
