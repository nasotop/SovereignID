## ADDED Requirements

### Requirement: Tab Reportes en portal Academy

El portal `/academy` SHALL exponer una tab **Reportes** junto a Estudiantes y Usuarios cuando el usuario tiene una institución seleccionada.

La tab Reportes SHALL ser visible para roles institucionales `admin`, `issuer` y `viewer`, y para `platform_admin` con membership o acceso cross-tenant, sin requerir `canManageInstitution()`.

El contenido de la tab SHALL renderizarse en un componente hijo standalone `academy-reports-tab` que recibe `institutionId` como input obligatorio.

#### Scenario: Viewer accede a reportes de su institución

- **WHEN** un usuario con membership `{institutionId}:viewer` selecciona una institución y activa la tab Reportes
- **THEN** el portal monta `academy-reports-tab` con ese `institutionId`
- **AND** la tab Reportes es accesible sin mostrar la tab Usuarios

#### Scenario: Tab oculta sin institución seleccionada

- **WHEN** el usuario no tiene `selectedInstitution` en el portal Academy
- **THEN** la tab Reportes no está disponible o no muestra contenido de reportes institution-scoped

### Requirement: Consumo de reportes institution-scoped vía BFF

`academy-reports-tab` SHALL consumir los cinco reportes institution (R-I1…R-I5) exclusivamente a través de la fachada `ReportsService`, invocando rutas `/api/reports/institutions/{institutionId}/…` definidas en el contrato BFF.

El componente MUST NOT leer `HttpErrorResponse` ni el cuerpo HTTP directamente.

#### Scenario: Carga de los cinco reportes al entrar en la tab

- **WHEN** el usuario entra en la tab Reportes con institución seleccionada y período default (30 días)
- **THEN** el componente dispara en paralelo las peticiones R-I1, R-I2, R-I3, R-I4 y R-I5 con el `institutionId` activo
- **AND** usa `from`/`to` del selector de período para R-I1, R-I2, R-I4, R-I5
- **AND** usa `asOf = to` para R-I3

### Requirement: Visualización institution en tab Reportes

La tab Reportes SHALL renderizar:
- **R-I1** (credenciales emitidas): gráfico de línea G2 + total del período + badge de `source`
- **R-I2** (verificaciones): gráfico de línea G2 + total; etiqueta visible **Verificaciones** (MUST NOT usar "leídas" en UI)
- **R-I3** (promedio credenciales por alumno): fila de KPI cards (`averageCredentialsPerStudent`, `totalStudents`, `totalCredentials`)
- **R-I4** (credenciales revocadas): gráfico de línea G2 + total + badge de `source`
- **R-I5** (resultados de verificación): gráfico G2 con series agrupadas válidas (`emerald`) e inválidas (`red`) + totales

#### Scenario: Serie temporal renderizada con G2

- **WHEN** R-I1 responde `200` con `series[]` y `total`
- **THEN** el portal muestra un gráfico de línea AntV G2 con tema oscuro
- **AND** muestra el total numérico del período
- **AND** muestra el valor de `source` (`snapshot`, `live` o `hybrid`)

#### Scenario: Etiqueta Verificaciones para R-I2

- **WHEN** el reporte R-I2 se muestra en la UI
- **THEN** el título visible del bloque es **Verificaciones** o equivalente en español que no diga "leídas"

### Requirement: Recarga al cambiar institución o período

`academy-reports-tab` SHALL recargar todos los reportes cuando cambia el input `institutionId` o cuando el usuario selecciona un preset de período distinto (7, 30 o 90 días).

#### Scenario: Cambio de institución recarga reportes

- **WHEN** el usuario cambia la institución activa mientras permanece en la tab Reportes
- **THEN** el componente hijo recibe el nuevo `institutionId` y relanza la carga completa de los cinco reportes

### Requirement: Error aislado por reporte en Academy

Si una petición de reporte falla, las demás secciones cargadas SHALL permanecer visibles. La sección fallida SHALL mostrar el `detail` del Problem Details vía seam de errores y un control de reintento que relanza solo ese reporte.

#### Scenario: Fallo parcial no oculta otros reportes

- **WHEN** R-I2 responde error y R-I1 respondió `200`
- **THEN** R-I1 permanece visible con su gráfico
- **AND** R-I2 muestra mensaje de error y botón Reintentar
