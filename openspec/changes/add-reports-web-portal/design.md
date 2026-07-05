## Context

El backend de reportería ya está implementado (`reports-api`, pass-through BFF, contrato en `docs/contracts/bff.openapi.json` y `docs/contracts/reports.openapi.json`). Los portales Angular `/academy` y `/platform` no consumen esos endpoints: `AcademyComponent` (~925 líneas) gestiona estudiantes/usuarios; `PlatformComponent` (~671 líneas) gestiona instituciones sin tabs.

Restricciones vigentes:
- Cliente HTTP generado desde OpenAPI + fachada manual con seam de Problem Details ([ADR-0004](../../../docs/adr/0004-openapi-client-codegen.md)).
- BFF pass-through; el front llama `/api/reports/…` (nginx strip → `bff-api`).
- Design system: consola institucional, Tailwind, acento azul (Academy) / violeta (Platform), sin capas visuales innecesarias ([`docs/web-design-system.md`](../../../docs/web-design-system.md)).
- Matriz RBAC: ver reportes permitido para `platform_admin`, `admin`, `issuer`, `viewer` ([`docs/authorization-domain-contract.md`](../../../docs/authorization-domain-contract.md)).

Decisions cerradas en sesión de diseño (grill-with-docs):

| Tema | Decisión |
|------|----------|
| Visualización | AntV **G2 v5** (`theme: classicDark`) para series y rankings |
| Período | Presets **7 / 30 / 90 días**, default **30**; `asOf = to` para R-I3 y R-P2 |
| Platform UI | Tab principal **Instituciones \| Reportes** |
| Componentización | Refactor **mínimo**: `academy-reports-tab`, `platform-institutions-tab`, `platform-reports-tab`; Estudiantes/Usuarios permanecen en shell Academy |
| Visibilidad Academy | Tab Reportes para **admin, issuer, viewer** (como Estudiantes) |
| Carga/errores | **`Promise.allSettled`** paralelo; error aislado por tarjeta + reintento local |
| Etiqueta R-I2 | UI: **Verificaciones** (no "leídas") |

## Goals / Non-Goals

**Goals:**
- Consumir los 7 reportes desde el portal web vía BFF con tipos generados y fachada `ReportsService`.
- Tab Reportes en Academy (R-I1…R-I5) y tabs Instituciones | Reportes en Platform (R-P1, R-P2).
- Componentes hijos standalone para tabs nuevas/extraídas; UI compartida en `shared/ui/reports/`.
- Gráficos G2 para series temporales y rankings; KPI cards para R-I3.
- Selector de período compartido por portal; carga paralela resiliente.

**Non-Goals:**
- Refactor completo de tabs Estudiantes/Usuarios en Academy (cambio posterior).
- Rango de fechas personalizado (solo presets 7/30/90 en v1).
- Portal Issuer con métricas (mencionado en backend como futuro).
- Cambios en `reports-api`, BFF, contratos OpenAPI backend, ni job de snapshot.
- Export CSV/PDF, drill-down por credencial, ni alertas.

## Decisions

### D1: Cliente BFF regenerado + fachada `ReportsService`
Regenerar `src/app/api/bff/` desde `docs/contracts/bff.openapi.json` (rutas `/reports/…` ya en snapshot). Fachada manual envuelve el cliente generado, aplica `error.utils.ts`, expone métodos tipados por reporte (R-I1…R-P2). Componentes hablan solo con la fachada.
**Alternativa:** HttpClient manual con tipos escritos a mano — contradice ADR-0004 y duplica el contrato ya versionado en BFF OpenAPI.

### D2: AntV G2 v5 como librería de charts
Dependencia `@antv/g2` en `src/web`. Wrapper `report-chart.component.ts` encapsula inicialización/destrucción del chart, `theme: 'classicDark'`, modos `line`, `grouped-line` (R-I5), `bar-horizontal` (R-P1, R-P2).
**Alternativas:** (a) barras CSS Tailwind — insuficiente para series multi-día con tooltips; (b) ngx-charts — menos alineado con guía AntV ya adoptada en skills del repo.

### D3: Componentización mínima por tabs
| Componente | Responsabilidad |
|------------|-----------------|
| `academy.component.ts` | Shell: institución activa, nav tabs (Estudiantes \| Usuarios \| Reportes), monta hijos |
| `academy-reports-tab.component.ts` | Input `institutionId`; período + 5 reportes |
| `platform.component.ts` | Shell: nav tabs (Instituciones \| Reportes) |
| `platform-institutions-tab.component.ts` | Contenido actual de gestión de tenants (extraído) |
| `platform-reports-tab.component.ts` | Período + R-P1 + R-P2 |
| `shared/ui/reports/*` | Presentacionales reutilizables |

Estudiantes/Usuarios permanecen inline en Academy para limitar el diff.
**Alternativa:** refactor completo de Academy — mayor alcance sin beneficio inmediato para reportes.

### D4: Selector de período compartido
Un `report-period-selector` emite `{ from, to }` en formato `YYYY-MM-DD`. Presets: 7, 30 (default), 90 días inclusive hasta hoy (UTC/local consistente con backend `DateOnly`). Reportes puntuales usan `asOf = to`.
**Alternativa:** rango custom — fuera de alcance v1; backend limita a 366 días pero presets cubren el 95% operacional.

### D5: Carga paralela con error aislado
Al montar tab o cambiar período/institutionId: `Promise.allSettled` sobre todas las peticiones del portal. Cada sección tiene estado `idle | loading | loaded | error`. Reintento relanza solo esa petición. Spinner global solo mientras **todas** están en loading inicial.
**Alternativa:** error global — un fallo parcial ocultaría KPIs válidos.

### D6: Lazy mount de tabs de reportes
`@if (activeTab() === 'reports')` monta el componente hijo; primera visita dispara fetch. Al cambiar tab, el hijo se destruye (estado efímero) o se preserva con `@if` + flag — preferir **destruir y recargar** en v1 (simplicidad).
**Alternativa:** cache en servicio — optimización prematura.

## Architecture (wireframes)

### Academy — tab Reportes

```
┌─ Estudiantes | Usuarios | Reportes ─────────────────────────────┐
│ Institución activa: Duoc UC                                      │
│ [7d] [30d] [90d]                                                 │
├─────────────────────────────────────────────────────────────────┤
│ R-I3 KPI row: Promedio | Alumnos | Credenciales                 │
│ R-I1 Emisiones          [line chart G2]  Total: N  [source]     │
│ R-I2 Verificaciones     [line chart G2]  Total: N  [source]     │
│ R-I4 Revocaciones       [line chart G2]  Total: N  [source]     │
│ R-I5 Válidas vs inválidas [grouped line]  [source]              │
└─────────────────────────────────────────────────────────────────┘
```

### Platform — tab Reportes

```
┌─ Instituciones | Reportes ──────────────────────────────────────┐
│ [7d] [30d] [90d]                                                 │
├─────────────────────────────────────────────────────────────────┤
│ R-P1 Credenciales por institución  [horizontal bar G2]          │
│ R-P2 Alumnos por institución       [horizontal bar G2]          │
└─────────────────────────────────────────────────────────────────┘
```

## Risks / Trade-offs

- [Cliente BFF desactualizado sin rutas reports en codegen] → Primera tarea: `npm run gen:api:bff` y commitear diff.
- [Bundle size por G2] → Wrapper único; charts solo en tabs Reportes (lazy mount).
- [Academy sigue creciendo (~925 líneas)] → Aceptado en v1; refactor Estudiantes/Usuarios diferido explícitamente.
- [G2 lifecycle en Angular (resize, destroy)] → `report-chart` implementa `ngOnDestroy` + `ResizeObserver` o `autoFit`.
- [Sin datos snapshot en dev InMemory] → Documentar que Postgres + backfill snapshot mejora `source: hybrid`; live tail funciona igual.

## Migration Plan

1. Añadir `@antv/g2`; regenerar cliente BFF.
2. Implementar `shared/ui/reports/*` y `ReportsService`.
3. Extraer `platform-institutions-tab`; añadir tabs y `platform-reports-tab`.
4. Añadir tab Reportes y `academy-reports-tab` en Academy.
5. Tests unitarios de fachada y tabs; verificación manual con stack Docker + seed-dev.

Rollback: revertir commit; portales vuelven sin tab Reportes; backend no afectado.

## Open Questions

- Ninguna bloqueante (decisiones cerradas en grill-with-docs).
