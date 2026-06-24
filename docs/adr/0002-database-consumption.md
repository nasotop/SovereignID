# ADR-0002: Reglas de consumo de base de datos (.NET)

## Estado

Aceptado — 2026-06-23

## Contexto

El monorepo SovereignID usa PostgreSQL 16 con esquema canónico en `database/BBDD_SovereignID.sql`. El microservicio `auth` ya tiene modelo EF generado (`SovereignIdDbContext`) y un primer adapter (`PostgresChallengeStore` sobre `IChallengeStore`).

Sin reglas explícitas, el riesgo es:

1. Inyectar `DbContext` en casos de uso o controllers (acoplamiento a EF y al esquema completo).
2. Usar EF Code-First y generar drift respecto al SQL canónico.
3. Mezclar entidades EF con tipos de dominio en Application.
4. Romper tests de integración al exigir Postgres en CI.

## Decisión

### Fuente de verdad del esquema

| Regla | Detalle |
|-------|---------|
| **SQL canónico** | `database/BBDD_SovereignID.sql` define tablas, ENUMs, índices y comentarios. |
| **Prohibido Code-First** | No usar `dotnet ef migrations add` ni modificar el esquema desde C#. |
| **Regeneración** | Tras cambiar el SQL: `make db-reset` (o equivalente) + `scripts/scaffold-auth-db.ps1` (EF Core Power Tools CLI). |

### Capas y seams

| Capa | Puede consumir BD | No puede |
|------|-------------------|----------|
| **Domain** | Nada | EF, connection strings, entidades scaffold |
| **Application** | Interfaces de persistencia propias del servicio (p. ej. `IChallengeStore`) | `DbContext`, `DbSet`, tipos en `Persistence.Generated.Entities` |
| **Infrastructure** | `DbContext`, entidades EF, adapters | Exponer `DbContext` fuera del ensamblado |
| **Api** | Configuración (`ConnectionStrings`, `Persistence:Provider`) | Referenciar tipos EF ni inyectar `DbContext` |

**Regla del seam:** cada agregado o tabla que un caso de uso necesite se accede mediante una **interfaz en Application** y un **adapter en Infrastructure**. El adapter concentra mapeo dominio ↔ fila, `SaveChanges` y atomicidad.

**Referencia de implementación:** `PostgresChallengeStore` → `IChallengeStore` → `auth_challenges`.

### DbContext

- `SovereignIdDbContext` es **`internal`** a `Auth.Infrastructure`. Tras cada scaffold, el script de regeneración debe restaurar `internal`.
- El DbContext scaffold incluye las 12 tablas del MVP; el servicio `auth` **solo debe consultar/escribir las tablas que le pertenecen** vía adapters dedicados (hoy: `auth_challenges`).
- Registro DI: `AddAuthPostgresPersistence()` — scoped, Npgsql, connection string desde `ConnectionStrings:DefaultConnection`.
- No registrar ni inyectar `SovereignIdDbContext` en `Program.cs` de Api.

### Dominio vs entidades EF

- Los tipos en `Auth.Infrastructure.Persistence.Generated.Entities` son **filas de BD**, no modelos de dominio.
- Si el nombre colisiona (p. ej. `AuthChallenge`), el dominio vive en `Auth.Domain`; la entidad EF no se reutiliza en Application.
- Rehidratación desde BD: factories estáticas en dominio (p. ej. `AuthChallenge.Rehydrate`) invocadas solo desde adapters.

### Configuración y proveedores

| Clave | Valores | Uso |
|-------|---------|-----|
| `Persistence:Provider` | `InMemory` (default) \| `Postgres` | Selector de adapter |
| `ConnectionStrings:DefaultConnection` | cadena Npgsql | Obligatoria si `Provider=Postgres` |

- **InMemory:** `IChallengeStore` como singleton; sin Postgres al arranque.
- **Postgres:** `IChallengeStore` scoped (misma vida que `DbContext`).
- Desde contenedor Docker: `Host=postgres;Port=5432;…`. Desde host: cuidado con colisión de puerto 5432 con Postgres local.

Validación en arranque: `ValidateAuthConfiguration()` falla si `Postgres` está activo sin connection string.

### Escrituras y concurrencia

- Operaciones de un solo uso (consumir auth challenge) deben ser **atómicas en BD** (`ExecuteUpdate` con condición `consumed_at IS NULL`, o transacción equivalente). No confiar en leer-modificar-guardar sin condición.
- Lecturas de validación previas pueden usar `AsNoTracking`.
- Timestamps: persistir en UTC; rehidratar como `DateTimeOffset` UTC.

### Tests

- Tests de integración del servicio auth **usan `Persistence:Provider=InMemory`** por defecto (`AuthWebApplicationFactory`).
- No exigir Docker/Postgres en CI para AC-01…AC-07.
- Tests que validen el adapter Postgres (si se añaden) serán opt-in con infraestructura explícita.

### Archivos generados

- Código bajo `Persistence/Generated/` generado por `efcpt` **no se edita a mano** salvo:
  - `internal` en `SovereignIdDbContext` (post-scaffold).
- Lógica de negocio y mapeo viven en adapters bajo `Persistence/Stores/` (p. ej. `Stores/ChallengeStore/PostgresChallengeStore.cs`).

## Consecuencias

### Positivas

- Application permanece testeable sin BD.
- Un solo lugar por tabla/agregado para mapeo y atomicidad.
- El esquema SQL sigue siendo la única fuente de verdad.
- Activar Postgres en producción no requiere cambiar casos de uso.

### Negativas

- Cada nuevo consumo de BD implica interfaz + adapter (más archivos que inyectar `DbContext` directamente).
- Regenerar el modelo puede requerir re-aplicar `internal` en el DbContext.
- El DbContext monolítico scaffold puede confundir; la disciplina de “solo tablas propias vía adapter” es convención, no enforced por el compilador.

## Referencias

- [ADR-0001](0001-problem-details-errors.md) — errores HTTP (independiente de persistencia)
- [`CONTEXT.md`](../../CONTEXT.md) — mapa del monorepo y glosario
- `database/BBDD_SovereignID.sql` — esquema canónico
- `scripts/scaffold-auth-db.ps1` — regeneración database-first
- `src/auth/Auth.Infrastructure/Persistence/Stores/ChallengeStore/PostgresChallengeStore.cs` — adapter de referencia
