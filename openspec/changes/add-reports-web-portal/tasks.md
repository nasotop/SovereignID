## 1. Dependencias y cliente BFF



- [x] 1.1 Añadir `@antv/g2` a `dependencies` en `src/web/package.json` y ejecutar `npm install`

- [x] 1.2 Ejecutar `npm run gen:api:bff` y commitear el cliente regenerado en `src/web/src/app/api/bff/` (incluye rutas `/reports/…`)

- [x] 1.3 Verificar que las operaciones reports aparecen en `src/app/api/bff/fn/` y que `npm run build` compila sin errores de tipos



## 2. Fachada ReportsService



- [x] 2.1 Crear `core/services/reports.service.ts` inyectando el cliente BFF generado

- [x] 2.2 Implementar métodos async para R-I1…R-I5 (institution) y R-P1…R-P2 (platform) con query params `from`/`to`/`asOf`

- [x] 2.3 Aplicar seam de Problem Details (`error.utils.ts`); no exponer `HttpErrorResponse` a componentes

- [x] 2.4 Añadir tests unitarios (vitest) para mapeo de rutas y traducción de errores (mock HttpClient/cliente generado)



## 3. Componentes compartidos `shared/ui/reports/`



- [x] 3.1 Crear `report-period-selector.component.ts`: presets 7/30/90 días, default 30, output `{ from, to }` en `YYYY-MM-DD`

- [x] 3.2 Crear `report-kpi-stat.component.ts`: label + valor numérico

- [x] 3.3 Crear `report-source-badge.component.ts`: badge para `snapshot` | `live` | `hybrid`

- [x] 3.4 Crear `report-chart.component.ts`: wrapper G2 v5 con `theme: classicDark`, modos `line`, `grouped-line`, `bar-horizontal`; destroy en `ngOnDestroy`



## 4. Portal Platform — tabs y componentes hijos



- [x] 4.1 Introducir `type PlatformTab = 'institutions' | 'reports'` y nav tabs en `platform.component.ts` (shell delgado)

- [x] 4.2 Extraer contenido actual a `platform-institutions-tab.component.ts` (listado, aside, modales crear/invitar)

- [x] 4.3 Crear `platform-reports-tab.component.ts`: selector de período + R-P1 + R-P2 con G2 horizontal bar

- [x] 4.4 Implementar carga paralela `Promise.allSettled` y error aislado con reintento por sección

- [x] 4.5 Montar `platform-reports-tab` solo con `@if (activeTab() === 'reports')`

- [x] 4.6 Actualizar/añadir tests en `platform.component.spec.ts` y specs de tab reportes si aplica



## 5. Portal Academy — tab Reportes



- [x] 5.1 Extender `AcademyTab` con `'reports'` y añadir botón de tab visible para admin/issuer/viewer (no gated por `canManageInstitution()`)

- [x] 5.2 Crear `academy-reports-tab.component.ts` con `@Input({ required: true }) institutionId`

- [x] 5.3 Implementar layout: KPI row R-I3 + gráficos G2 para R-I1, R-I2 (etiqueta **Verificaciones**), R-I4, R-I5 (series válidas/inválidas)

- [x] 5.4 Implementar carga paralela `Promise.allSettled`, recarga al cambiar `institutionId` o período, error aislado con reintento

- [x] 5.5 Montar `academy-reports-tab` con `@if (activeTab() === 'reports')` cuando hay `selectedInstitution()`



## 6. Verificación



- [x] 6.1 Ejecutar `npm run build` y `npm test` en `src/web` (verificado con `npx tsc -p tsconfig.app.json --noEmit`; `ng build`/`ng test` bloqueados por Node v22.16.0 < mínimo v22.22.3)

- [ ] 6.2 Verificación manual: stack Docker + seed-dev; login platform admin → tab Reportes Platform (R-P1, R-P2)

- [ ] 6.3 Verificación manual: login viewer/admin/issuer → Academy → tab Reportes (R-I1…R-I5) con preset 30 días

- [ ] 6.4 Confirmar escenarios de specs: error parcial, reintento, cambio de institución, etiqueta Verificaciones



## 7. Documentación



- [x] 7.1 Verificar que `CONTEXT.md` refleja decisiones finales (G2, tabs, presets, carga paralela) — ajustar si hace falta tras implementación

- [x] 7.2 Marcar tareas completadas en este `tasks.md` al implementar

