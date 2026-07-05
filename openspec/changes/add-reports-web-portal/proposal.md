## Why

El backend de reportería (`reports-api` + pass-through BFF en `/api/reports/…`) ya expone **7 KPIs** institution y platform con autorización JWT alineada a los portales, pero **no existe UI Angular** que los consuma. Los usuarios de `/academy` (admin, issuer, viewer) y `/platform` (platform admin) no pueden ver dashboards operativos sin llamar la API manualmente.

Este cambio cablea los reportes al portal web: tab **Reportes** en Academy (R-I1…R-I5) y tab **Reportes** en Platform (R-P1, R-P2), reutilizando el contrato BFF existente y el patrón de cliente generado + fachada (ADR-0004).

## What Changes

- Regenerar cliente BFF (`npm run gen:api:bff`) para incluir rutas `/reports/…` ya presentes en `docs/contracts/bff.openapi.json`.
- Nueva fachada **`ReportsService`** en `core/services/` con seam de Problem Details (`error.utils.ts`).
- Tab **Reportes** en `AcademyComponent`: visible para `admin`, `issuer` y `viewer` cuando hay institución seleccionada; contenido en **`academy-reports-tab`** (componente hijo standalone).
- Tabs **Instituciones | Reportes** en `PlatformComponent`: extraer gestión actual a **`platform-institutions-tab`**; reportes cross-tenant en **`platform-reports-tab`**.
- Componentes compartidos en **`shared/ui/reports/`**: selector de período (presets 7/30/90 días, default 30), wrapper G2 v5 (`classicDark`), KPI stat, badge de `source`.
- Visualización con **AntV G2 v5** para series temporales (R-I1, R-I2, R-I4, R-I5) y rankings (R-P1, R-P2); R-I3 como KPI cards.
- Carga paralela con **`Promise.allSettled`**; error aislado por tarjeta con reintento local.
- Etiqueta UI **Verificaciones** para R-I2 (no "leídas").
- Nueva dependencia: **`@antv/g2`** en `src/web`.

## Capabilities

### New Capabilities

- `academy-reports-web-portal`: Tab Reportes en portal `/academy` consumiendo reportes institution-scoped (R-I1…R-I5) vía BFF, con selector de período compartido, G2 para series, KPI para R-I3, y visibilidad para admin/issuer/viewer.
- `platform-reports-web-portal`: Tabs Instituciones | Reportes en portal `/platform`; tab Reportes consume R-P1 y R-P2; refactor mínimo extrayendo listado de instituciones a componente hijo.
- `reports-web-ui`: Componentes compartidos de reportería (`report-period-selector`, `report-chart`, `report-kpi-stat`, `report-source-badge`) y fachada `ReportsService`.

### Modified Capabilities

- _(ninguna — el backend y contratos HTTP de reports no cambian; solo se consume desde el front)_

## Impact

- **Frontend (web):** `src/web/package.json` (`@antv/g2`), `src/app/api/bff/` (regenerado), `core/services/reports.service.ts`, `features/portals/academy/` (shell + `academy-reports-tab`), `features/portals/platform/` (shell + `platform-institutions-tab` + `platform-reports-tab`), `shared/ui/reports/**`.
- **Backend / BFF / Infra:** sin cambios (rutas ya expuestas).
- **Docs:** `CONTEXT.md` (sección frontend reportes — ya actualizada en sesión de diseño).
- **Tests:** specs de componentes para tabs de reportes y fachada (vitest), opcional e2e manual.
