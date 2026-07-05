## ADDED Requirements

### Requirement: Tabs Instituciones y Reportes en portal Platform

El portal `/platform` SHALL exponer navegación por tabs **Instituciones** y **Reportes** usando el patrón `signal<PlatformTab>` y control flow `@if`, con acento visual violeta coherente con el design system.

La gestión actual de tenants (listado, detalle, crear institución, invitar admin) SHALL moverse al componente hijo standalone `platform-institutions-tab`.

Los reportes cross-tenant SHALL renderizarse en `platform-reports-tab`, montado solo cuando la tab Reportes está activa.

#### Scenario: Platform admin alterna entre tabs

- **WHEN** un usuario `platform_admin` accede a `/platform`
- **THEN** ve tabs Instituciones (activa por default) y Reportes
- **AND** al activar Reportes se monta `platform-reports-tab` con ancho completo del main

#### Scenario: Extracción del listado de instituciones

- **WHEN** la tab Instituciones está activa
- **THEN** el contenido equivalente al listado y panel lateral actual se renderiza desde `platform-institutions-tab`
- **AND** `platform.component.ts` actúa como shell de navegación sin lógica de CRUD inline

### Requirement: Consumo de reportes platform-scoped vía BFF

`platform-reports-tab` SHALL consumir R-P1 (`credentials-by-institution`) y R-P2 (`students-by-institution`) exclusivamente vía `ReportsService` y rutas `/api/reports/platform/…`.

#### Scenario: Carga paralela de reportes platform

- **WHEN** el usuario entra en la tab Reportes con período default (30 días)
- **THEN** el componente dispara en paralelo R-P1 con `from`/`to` y R-P2 con `asOf = to`
- **AND** procesa resultados con `Promise.allSettled`

### Requirement: Visualización platform en tab Reportes

La tab Reportes SHALL renderizar:
- **R-P1**: gráfico de barras horizontal G2 ranking instituciones por total de credenciales emitidas en el período + badge de `source`
- **R-P2**: gráfico de barras horizontal G2 ranking instituciones por stock de alumnos activos al `asOf`

Cada ranking SHALL incluir `displayName` legible y valor numérico.

#### Scenario: Ranking R-P1 renderizado

- **WHEN** R-P1 responde `200` con `items[]` ordenados por `total` descendente
- **THEN** el portal muestra un gráfico de barras horizontal AntV G2
- **AND** cada barra corresponde a una institución con su `displayName`

### Requirement: Error aislado por reporte en Platform

Si R-P1 o R-P2 falla, la sección exitosa SHALL permanecer visible. La sección fallida SHALL mostrar `detail` del Problem Details y control de reintento local.

#### Scenario: Fallo de R-P2 con R-P1 exitoso

- **WHEN** R-P1 responde `200` y R-P2 responde error
- **THEN** R-P1 permanece visible
- **AND** R-P2 muestra error con opción de reintento
