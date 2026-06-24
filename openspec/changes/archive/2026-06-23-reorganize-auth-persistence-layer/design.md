## Context

`Auth.Infrastructure` concentra adapters SIWE/JWT en la raíz y persistencia EF en `Persistence/`. Hoy esa carpeta mezcla:

| Tipo | Archivos | Origen |
|------|----------|--------|
| Entidades EF (12) + `SovereignIdDbContext` | `Persistence/*.cs` | efcpt (no editar) |
| Adapter Postgres | `PostgresChallengeStore.cs` | escrito a mano |
| DI / config | `PersistenceServiceCollectionExtensions.cs`, `PersistenceOptions.cs` | escrito a mano |

`InMemoryChallengeStore` vive en la raíz del ensamblado; `PostgresChallengeStore` en `Persistence/`. `DependencyInjection.AddAuthInfrastructure()` registra SIWE, casos de uso y selector `InMemory` | `Postgres` en un solo método.

ADR-0002 ya define el seam Application → adapter → EF. Este cambio **no altera comportamiento** de auth challenges ni contratos HTTP; solo reorganiza la **localidad física** para reforzar esa disciplina.

Restricciones:

- `SovereignIdDbContext` debe permanecer `internal` tras cada scaffold.
- efcpt es la herramienta de regeneración (`efcpt-config.json`, `scripts/scaffold-auth-db.ps1`).
- Tests de integración usan `InMemory` por defecto; deben seguir pasando sin cambios de configuración.

## Goals / Non-Goals

**Goals:**

- Separar visualmente código **generado** de código **autorado** bajo `Persistence/`.
- Colocar ambos adapters de `IChallengeStore` en la misma carpeta con visibilidad coherente.
- Extraer composición DI de persistencia en un módulo dedicado invocable desde `AddAuthInfrastructure()`.
- Actualizar tooling y `CONTEXT.md` para que la convención sea discoverable.

**Non-Goals:**

- Cambiar requisitos de `auth-challenge`, SIWE o JWT.
- Añadir nuevos adapters de tablas (`users`, `credentials`, …).
- Extraer mappers dedicados (oportunidad #4 del review arquitectónico).
- Reorganizar adapters SIWE/JWT en subcarpetas (oportunidad #6).
- Modificar ADR-0002 (solo referencias de rutas en documentación si aplica).

## Decisions

### 1. Layout de carpetas bajo `Persistence/`

**Decisión:**

```
Auth.Infrastructure/
├── DependencyInjection.cs              # SIWE, JWT, use cases; llama AddAuthPersistence()
├── SiweMessageParser.cs                  # sin mover (Non-Goal #6)
├── JwtBearerTokenIssuer.cs
├── PersonalSignSignatureVerifier.cs
└── Persistence/
    ├── Generated/                        # SOLO efcpt — no editar a mano
    │   ├── SovereignIdDbContext.cs
    │   └── Entities/
    │       ├── AuthChallenge.cs
    │       └── … (11 entidades restantes)
    ├── Stores/
    │   └── ChallengeStore/
    │       ├── InMemoryChallengeStore.cs
    │       └── PostgresChallengeStore.cs
    └── Composition/
        ├── PersistenceOptions.cs
        ├── PersistenceServiceCollectionExtensions.cs   # AddAuthPostgresPersistence, UsesPostgresPersistence
        └── AuthPersistenceServiceCollectionExtensions.cs  # AddAuthPersistence (nuevo)
```

**Alternativas consideradas:**

- *Subcarpeta `Entities/` plana sin `Generated/`* — descartada: no distingue generado de escrito.
- *Proyecto ensamblado separado para entidades EF* — descartada: overhead prematuro para un solo servicio.

**Rationale:** El layout hace obvio qué borrar/regenerar y dónde añadir adapters futuros (`Stores/<Aggregate>/`).

### 2. Namespaces alineados con carpetas

**Decisión:**

| Ubicación | Namespace |
|-----------|-----------|
| `Generated/SovereignIdDbContext.cs` | `Auth.Infrastructure.Persistence.Generated` |
| `Generated/Entities/*.cs` | `Auth.Infrastructure.Persistence.Generated.Entities` |
| `Stores/ChallengeStore/*.cs` | `Auth.Infrastructure.Persistence.Stores.ChallengeStore` |
| `Composition/*.cs` | `Auth.Infrastructure.Persistence.Composition` |

**Alternativa descartada:** Mantener namespace `Persistence.Entities` con archivos en `Generated/` — reduce churn de usings pero contradice la regla “carpeta = namespace” y confunde navegadores.

**Rationale:** Los usings en adapters se actualizan una vez; el beneficio de coherencia supera el coste.

### 3. Visibilidad unificada de stores

**Decisión:** Ambos adapters `IChallengeStore` serán `internal sealed`. El registro DI ocurre en el mismo ensamblado; tests no referencian los tipos concretos.

**Alternativa descartada:** Mantener `InMemoryChallengeStore` como `public` — no hay consumidores externos; asimetría sin beneficio.

### 4. Módulo `AddAuthPersistence()`

**Decisión:** Nuevo método de extensión en `AuthPersistenceServiceCollectionExtensions`:

```csharp
public static IServiceCollection AddAuthPersistence(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

    if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
    {
        services.AddAuthPostgresPersistence(configuration);
        services.AddScoped<IChallengeStore, PostgresChallengeStore>();
    }
    else
    {
        services.AddSingleton<IChallengeStore, InMemoryChallengeStore>();
    }

    return services;
}
```

`DependencyInjection.AddAuthInfrastructure()` elimina la lógica de persistencia y delega:

```csharp
services.AddAuthPersistence(configuration);
```

**Rationale:** Cualquier nueva tabla/adapters Postgres tocan solo `Persistence/Composition/` y `Persistence/Stores/`; SIWE queda aislado.

### 5. Configuración efcpt

**Decisión:** Actualizar `efcpt-config.json`:

```json
"file-layout": {
  "output-dbcontext-path": null,
  "output-path": "Persistence/Generated"
}
```

Ajustar `names.model-namespace` a `Persistence.Generated.Entities` y namespace del DbContext vía convención efcpt (`Persistence.Generated`).

**Rationale:** Regeneración escribe directamente en zona inmutable; `soft-delete-obsolete-files` limpia entidades movidas.

### 6. Script post-scaffold

**Decisión:** `scaffold-auth-db.ps1` apunta a `Persistence/Generated/SovereignIdDbContext.cs` para restaurar `internal`.

**Rationale:** Paso manual documentado en ADR-0002; la ruta debe coincidir con el layout nuevo.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| efcpt no respeta subcarpeta `false`Entities/` dentro de `Generated/` | Verificar tras primer scaffold; si efcpt aplana, aceptar entidades en `Generated/` con namespace `Generated.Entities` o ajustar config T4 |
| Diff grande por movimiento de archivos | Commit único de reorganización; sin cambios lógicos en stores |
| Referencias rotas en docs/ADR | Actualizar rutas en `CONTEXT.md` y referencias en ADR-0002 (paths, no decisiones) |
| Namespace change rompe build | `dotnet build` + `dotnet test` como gate obligatorio en tasks |

## Migration Plan

1. Crear estructura de carpetas vacía (`Generated/Entities`, `Stores/ChallengeStore`, `Composition`).
2. Mover archivos de composición y stores; actualizar namespaces y usings.
3. Mover entidades + DbContext a `Generated/` (o regenerar con efcpt tras actualizar config).
4. Implementar `AddAuthPersistence()` y simplificar `DependencyInjection.cs`.
5. Actualizar `efcpt-config.json` y `scaffold-auth-db.ps1`.
6. Actualizar `CONTEXT.md`.
7. Validar: `dotnet build`, `dotnet test tests/auth/Auth.IntegrationTests`.

**Rollback:** Revertir el commit de reorganización; sin impacto en datos ni despliegue.

## Open Questions

- _(ninguna bloqueante)_ — Si efcpt 10.x no crea subcarpeta `Entities/` automáticamente, aceptar layout plano en `Generated/` en la primera implementación y documentarlo en `CONTEXT.md`.
