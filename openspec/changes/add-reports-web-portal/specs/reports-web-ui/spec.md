## ADDED Requirements

### Requirement: Fachada ReportsService sobre cliente BFF generado

El frontend SHALL regenerar el cliente BFF desde `docs/contracts/bff.openapi.json` (`npm run gen:api:bff`) para incluir operaciones `/reports/…`.

El frontend SHALL implementar `ReportsService` en `core/services/reports.service.ts` que envuelve el cliente generado, aplica el seam de Problem Details (`error.utils.ts`) y expone métodos async tipados para R-I1…R-P2.

Los componentes de reportes MUST hablar únicamente con `ReportsService`.

#### Scenario: Llamada institution-scoped tipada

- **WHEN** `ReportsService.getCredentialsIssued(institutionId, from, to)` es invocado
- **THEN** realiza GET a `/api/reports/institutions/{institutionId}/credentials-issued` con query params de fecha
- **AND** devuelve el DTO tipado o lanza error traducido por el seam

### Requirement: Selector de período compartido

El componente `report-period-selector` SHALL ofrecer presets **7**, **30** (default) y **90** días inclusive hasta la fecha actual, emitiendo `{ from, to }` en formato `YYYY-MM-DD`.

Los tabs de reportes (Academy y Platform) SHALL usar un único selector por portal que aplica el mismo rango a todos los reportes del tab.

#### Scenario: Preset default 30 días

- **WHEN** el usuario abre una tab de reportes sin haber cambiado el período
- **THEN** el selector muestra 30 días activo
- **AND** `from` es hoy menos 29 días e inclusive `to` es hoy (30 días calendario inclusive)

#### Scenario: Cambio de preset recarga reportes

- **WHEN** el usuario selecciona preset 7 días
- **THEN** el selector emite nuevo `{ from, to }`
- **AND** el tab padre/hijo recarga todas las peticiones del portal con ese rango

### Requirement: Wrapper G2 report-chart

El componente `report-chart` SHALL encapsular AntV G2 v5 con `theme: classicDark`, soportando modos:
- `line`: series `{ time, value }` (R-I1, R-I2, R-I4)
- `grouped-line`: series con grupo `{ time, value, group }` (R-I5)
- `bar-horizontal`: `{ category, value }` (R-P1, R-P2)

El componente SHALL destruir la instancia G2 en `ngOnDestroy` y adaptarse al contenedor (`autoFit` o equivalente).

#### Scenario: Chart se destruye al desmontar tab

- **WHEN** el usuario sale de la tab Reportes
- **THEN** las instancias G2 creadas por `report-chart` se destruyen sin memory leak

### Requirement: Componentes auxiliares de reporte

El frontend SHALL proveer:
- `report-kpi-stat`: muestra label + valor numérico (R-I3 y totales de series)
- `report-source-badge`: muestra `source` ∈ { `snapshot`, `live`, `hybrid` } como metadata discreta

#### Scenario: Badge de source híbrido

- **WHEN** un reporte responde con `source: hybrid`
- **THEN** `report-source-badge` muestra la etiqueta **Hybrid** o **Híbrido** con estilo info/neutral del design system

### Requirement: Carga paralela con Promise.allSettled

Los tabs `academy-reports-tab` y `platform-reports-tab` SHALL lanzar todas sus peticiones de reporte en paralelo usando `Promise.allSettled`.

Cada reporte SHALL tener estado independiente (`loading`, `loaded`, `error`). Un fallo MUST NOT impedir renderizar los reportes exitosos.

#### Scenario: allSettled con un rechazo

- **WHEN** dos de cinco peticiones en Academy resuelven y tres fallan
- **THEN** las dos secciones exitosas renderizan datos
- **AND** las tres fallidas muestran error individual con reintento
