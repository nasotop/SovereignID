# SovereignID Web Design System

Este documento define el sistema visual y de layout para el proyecto Angular en `src/web`.
Debe usarse como referencia para nuevas pantallas, refactors de portales y componentes compartidos.

## Objetivo UX

SovereignID debe sentirse como una consola institucional: sobria, tecnica y eficiente. La prioridad no es promocionar el producto, sino permitir que usuarios autorizados gestionen instituciones, estudiantes, credenciales, wallets y verificaciones con bajo riesgo de error.

Principios:

- Operacional antes que promocional.
- Denso, pero legible.
- Cada accion critica debe tener estado, resultado y proximo paso claro.
- Wallet, DID, UUID, hashes y credential IDs son datos tecnicos: deben ser copiables, truncables y legibles.
- Las acciones con MetaMask deben verse distintas de las acciones normales del sistema.
- Evitar heroes, gradientes decorativos, cards promocionales y textos largos de ayuda dentro de la app.

## Stack UI Actual

`src/web` es una app Angular standalone con Tailwind CSS 4. Usa componentes con templates inline, Angular signals para estado local, clientes generados desde OpenAPI, `ethers` y `siwe` para login con MetaMask.

No hay una libreria visual formal. Los estilos se aplican con clases Tailwind repetidas. Por eso, el primer objetivo del sistema de diseno es estandarizar tokens, layout y componentes compartidos sin introducir una capa compleja.

## Paleta

### Neutrales

Usar `slate` como base unica. Evitar mezclar `gray` y `slate` en pantallas nuevas.

| Token | Hex | Tailwind | Uso |
|-------|-----|----------|-----|
| `surface.app` | `#020617` | `slate-950` | Fondo raiz cuando se requiera maxima profundidad |
| `surface.page` | `#0f172a` | `slate-900` | Fondo principal de app |
| `surface.panel` | `#1e293b` | `slate-800` | Paneles, formularios, tablas |
| `surface.raised` | `#334155` | `slate-700` | Hover, botones secundarios, superficies elevadas |
| `surface.field` | `#0f172a` | `slate-900` | Inputs y textareas |
| `border.subtle` | `#334155` | `slate-700` | Borde normal |
| `border.strong` | `#475569` | `slate-600` | Borde de campos e interacciones |

### Texto

| Token | Hex | Tailwind | Uso |
|-------|-----|----------|-----|
| `text.primary` | `#f8fafc` | `slate-50` | Titulos y texto principal |
| `text.secondary` | `#cbd5e1` | `slate-300` | Labels, descripcion corta |
| `text.muted` | `#94a3b8` | `slate-400` | Ayuda, metadata secundaria |
| `text.faint` | `#64748b` | `slate-500` | Placeholders, estados deshabilitados |

### Acentos

| Token | Hex | Tailwind | Uso |
|-------|-----|----------|-----|
| `action.primary` | `#2563eb` | `blue-600` | Accion primaria general |
| `action.primaryHover` | `#1d4ed8` | `blue-700` | Hover de accion primaria |
| `action.platform` | `#7c3aed` | `violet-600` | Platform admin, tenants, administracion global |
| `action.platformHover` | `#6d28d9` | `violet-700` | Hover platform |
| `action.wallet` | `#f97316` | `orange-500` | MetaMask, conectar wallet, firmar con wallet |
| `action.walletHover` | `#ea580c` | `orange-600` | Hover wallet |

Regla de dominio:

- Violeta: solo plataforma o administracion global.
- Azul: accion primaria normal en portales institucionales.
- Naranja: solo cuando la accion abre, conecta o depende de MetaMask.

### Estados

| Estado | Hex base | Tailwind recomendado | Uso |
|--------|----------|----------------------|-----|
| Success | `#10b981` | `emerald-500` | Activo, aceptado, valido |
| Warning | `#f59e0b` | `amber-500` | Expirado, pendiente, requiere atencion |
| Danger | `#ef4444` | `red-500` | Error, revocado, no autorizado |
| Info | `#0ea5e9` | `sky-500` | Informacion secundaria |
| Neutral | `#64748b` | `slate-500` | No evaluado, vacio, desconocido |

## Tipografia

Fuente principal:

```css
font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
```

Fuente tecnica:

```css
font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
```

Escala recomendada:

| Uso | Clases | Notas |
|-----|--------|-------|
| Marca/nav | `text-lg font-bold tracking-tight` | Solo en shell/header |
| Titulo de pagina | `text-2xl font-bold` | Un solo H1 por pantalla |
| Titulo de seccion | `text-xl font-semibold` | Bloques principales |
| Titulo de panel/card | `text-base font-semibold` | Evitar titulos grandes dentro de cards |
| Body | `text-sm leading-5` | Texto operativo normal |
| Small/meta | `text-xs leading-4` | Badges, timestamps, metadata |
| Tecnico | `font-mono text-xs` o `font-mono text-sm` | Wallet, DID, UUID, hashes |

Reglas:

- No escalar fuentes con viewport width.
- Letter spacing debe ser `0`, salvo `tracking-tight` en marca/nav.
- Usar espanol en portales institucionales y de plataforma. Mantener terminos tecnicos como wallet, DID, credential ID, hash, token.

## Layout

### Shell Autenticado

El layout base para `/platform`, `/academy`, `/issuer` y `/holder` debe usar `PortalShellComponent`.

Estructura:

- Fondo: `min-h-screen bg-slate-900 text-slate-100`.
- Header: altura aproximada 64px, `border-b border-slate-700/60 bg-slate-800/80 backdrop-blur-sm`.
- Contenedor: `mx-auto max-w-7xl px-6 py-8`.
- Gap vertical entre secciones: `space-y-6` o `gap-6`.
- En mobile: bajar a `px-4 py-6`.

El shell contiene marca, nombre del portal, resumen de sesion y logout. La pantalla solo define contenido.

### Vistas Internas Maestro-Detalle

Las vistas internas de administracion no deben crecer apilando formularios y paneles verticales. El patron base para Platform, Academy y futuros modulos internos es maestro-detalle:

1. Header de vista.
   - Izquierda: titulo de la vista y descripcion corta.
   - Derecha: acciones principales, por ejemplo `Crear institucion`, `Invitar usuario`, `Refrescar`.
   - Las acciones deben ser botones, no formularios visibles.

2. Contenido en dos columnas.
   - Columna izquierda: listado principal de elementos.
   - Columna derecha: panel de informacion.
   - Ambas columnas deben aprovechar al maximo el alto disponible del viewport.

3. Estado sin seleccion.
   - El panel derecho muestra informacion general de la vista.
   - Debe incluir KPIs, resumen operativo, datos rapidos, estados pendientes o ultimos cambios.
   - Ejemplo en instituciones: total de instituciones, activas, sin wallet emisor, invitaciones pendientes.

4. Estado con seleccion.
   - El panel derecho cambia a detalle puntual del elemento seleccionado.
   - Debe mostrar metadata, estado, acciones contextuales y relaciones relevantes.
   - Ejemplo en instituciones: codigo, pais, DID emisor, wallet emisor, fecha de registro, usuarios, estudiantes, acciones de invitacion.

5. Formularios.
   - Los formularios de creacion, edicion, invitacion o vinculacion no deben vivir como bloques permanentes en la pantalla.
   - Deben abrirse en modales, drawers o dialogs desde las acciones del header o desde el detalle.
   - El canvas principal queda reservado para navegar, comparar, inspeccionar y decidir.

Layout recomendado en desktop para vistas internas administrativas:

```html
<main class="mx-auto flex h-[calc(100vh-64px)] w-full max-w-none flex-col gap-6 px-6 py-6 2xl:px-8">
  <header class="flex items-start justify-between gap-4">
    <!-- titulo + descripcion -->
    <!-- acciones -->
  </header>

  <section class="grid min-h-0 flex-1 grid-cols-[minmax(0,1fr)_minmax(360px,440px)] gap-6">
    <aside class="min-h-0 overflow-hidden rounded-lg border border-slate-700 bg-slate-800">
      <!-- listado, filtros, tabla o cards compactas -->
    </aside>

    <aside class="min-h-0 overflow-auto rounded-lg border border-slate-700 bg-slate-800 p-6">
      <!-- KPIs si no hay seleccion, detalle si hay seleccion -->
    </aside>
  </section>
</main>
```

Responsive:

- En pantallas grandes, mantener dos columnas.
- En desktop administrativo, no centrar el contenido en `max-w-7xl`; usar ancho completo con padding lateral moderado.
- La pagina administrativa no debe generar scroll vertical global. El shell usa `h-screen overflow-hidden`; el listado y el panel lateral son los que pueden tener scroll interno.
- El padding inferior debe ser igual al lateral, normalmente `p-6` y `2xl:px-8`.
- En tablet, permitir `grid-cols-[minmax(0,1fr)_360px]` si el ancho lo soporta.
- En mobile, apilar listado y detalle; el detalle puede abrir como drawer/modal para evitar scroll excesivo.
- El listado debe tener filtros/busqueda pegados arriba si la lista crece.

Este patron reemplaza la estructura actual de Platform donde `Instituciones`, `Crear institucion` y `Consultar institucion` aparecen como bloques independientes. En el nuevo modelo, crear y consultar pasan a ser acciones, y el detalle vive en el panel derecho.

Para la vista Platform de instituciones, el listado izquierdo debe ser una tabla informativa, no cards. Columnas minimas:

- institucion: nombre, razon social e ID,
- codigo,
- pais,
- estado,
- wallet emisor,
- fecha de registro.

La accion `Abrir Academy` no debe repetirse en cada fila; debe vivir en el panel lateral cuando hay una institucion seleccionada.

Cuando un item esta seleccionado:

- el panel lateral muestra su detalle y un boton de cierre en la esquina superior derecha;
- seleccionar otro item reemplaza el detalle;
- seleccionar nuevamente el mismo item lo desmarca y vuelve a la vista general.

### Vistas Publicas

Login, invitaciones y verificador publico pueden usar layout centrado.

- Login/invitacion: `max-w-md` o `max-w-lg`.
- Verificador: `max-w-3xl`.
- Mantener fondo oscuro, pero permitir paneles mas destacados.
- `rounded-2xl` queda reservado para estas vistas publicas; no usarlo como default en consolas internas.

### Paneles, Cards y Tablas

Panel principal:

```html
<section class="rounded-lg border border-slate-700 bg-slate-800 p-6">
```

Card compacta:

```html
<article class="rounded-lg border border-slate-700 bg-slate-800 p-4">
```

Tabla:

- Header: `bg-slate-800 text-slate-300`.
- Filas: `border-t border-slate-700`.
- Celdas: `px-6 py-4`.
- Empty state dentro de tabla: texto centrado, `text-slate-400`, accion secundaria opcional.

Reglas:

- No poner cards dentro de cards.
- No usar secciones completas como cards flotantes si el shell ya da estructura.
- Cards solo para items repetidos, modales o herramientas claramente enmarcadas.
- En vistas maestro-detalle, el listado izquierdo puede usar tabla densa o cards compactas; no debe mezclar ambos patrones en la misma vista.
- El panel derecho debe tener una jerarquia clara: resumen arriba, metadata despues, acciones contextuales al final o en un bloque fijo.

## Componentes Base

### Botones

Tamanos:

| Tamano | Clases |
|--------|--------|
| `sm` | `px-3 py-2 text-sm` |
| `md` | `px-4 py-2.5 text-sm` |
| `lg` | `px-4 py-3 text-base` |

Variantes:

| Variante | Clases base | Uso |
|----------|-------------|-----|
| `primary` | `bg-blue-600 hover:bg-blue-700 text-white` | Accion principal |
| `platform` | `bg-violet-600 hover:bg-violet-700 text-white` | Platform admin |
| `wallet` | `bg-orange-500 hover:bg-orange-600 text-white` | MetaMask |
| `secondary` | `border border-slate-600 bg-slate-700 hover:bg-slate-600 text-white` | Cancelar, volver, descargar |
| `ghost` | `text-slate-300 hover:bg-slate-800 hover:text-white` | Accion secundaria ligera |
| `dangerText` | `text-red-400 hover:text-red-300` | Revocar, eliminar, bloquear |

Todos los botones deben tener:

- `inline-flex items-center justify-center gap-2`.
- `rounded-lg`.
- `font-semibold`.
- Estado disabled visible: `disabled:cursor-not-allowed disabled:opacity-50`.
- Focus: `focus:outline-none focus:ring-2 focus:ring-blue-500/30`.

### Formularios

Campo base:

```html
<label class="block text-sm font-medium text-slate-300">Nombre</label>
<input class="mt-1 w-full rounded-lg border border-slate-600 bg-slate-900 px-3 py-2 text-sm text-white placeholder:text-slate-500 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/30" />
```

Reglas:

- Mostrar helper/error bajo el campo cuando aplique.
- `font-mono` solo para valores tecnicos.
- Validar antes de enviar cuando el error sea predecible en cliente.
- Los formularios largos deben agruparse por secciones, no por cards anidadas.
- En portales internos, los formularios de creacion o edicion deben abrirse en `ModalComponent`, drawer o dialog. No deben quedar siempre visibles dentro del layout principal.
- Los modales deben tener titulo, descripcion breve, acciones primarias/secundarias y cierre claro.
- Para formularios de mas de 6 campos, preferir drawer o modal ancho con secciones internas.

### Badges

Base:

```html
<span class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium">
```

Estados:

- Activo/valido: `border-emerald-500/30 bg-emerald-500/15 text-emerald-400`.
- Pendiente/expirado: `border-amber-500/30 bg-amber-500/15 text-amber-400`.
- Error/revocado: `border-red-500/30 bg-red-500/15 text-red-400`.
- Neutral/no evaluado: `border-slate-500/40 bg-slate-500/20 text-slate-300`.

### Valores Tecnicos

Crear un componente `CopyValueComponent` para:

- wallet address,
- DID,
- UUID,
- credential ID,
- hash,
- CID,
- transaction hash.

Comportamiento esperado:

- Mostrar valor truncado por defecto.
- Permitir copiar.
- Tooltip o title con valor completo.
- Usar `font-mono`.
- No partir el layout en mobile.

### Iconos

Usar una estrategia unica. Recomendacion: `lucide-angular` si se agrega libreria. Mientras no exista, mantener un set minimo controlado y no duplicar SVG inline por pantalla.

Los botones de herramienta deben preferir icono con tooltip cuando la accion sea familiar; texto solo cuando el comando necesite claridad.

## Roles y Portales

### Platform Admin

Color de acento: violeta.

Debe poder:

- crear y listar instituciones,
- invitar usuarios institucionales,
- revisar membresias y roles,
- entrar a la administracion de una institucion,
- ver auditoria cuando exista.

Layout recomendado:

- Usar el patron maestro-detalle.
- Header: titulo `Instituciones` y acciones `Crear institucion`, `Refrescar`.
- Columna izquierda: listado de instituciones con busqueda/filtros.
- Panel derecho sin seleccion: KPIs de plataforma y datos rapidos.
- Panel derecho con seleccion: detalle de institucion, estado, usuarios, invitaciones y acciones contextuales.
- Los formularios `Crear institucion`, `Consultar institucion` e `Invitar usuario` deben ser modales/drawers, no secciones visibles permanentes.
- Tabs secundarios, si aparecen, deben vivir dentro del detalle o bajo el header, no fragmentar la pagina en multiples formularios.

### Academy Admin Institucional

Color de acento: azul.

Debe poder:

- gestionar estudiantes,
- vincular wallet manualmente a estudiante,
- gestionar usuarios institucionales,
- crear carreras cuando exista endpoint final,
- ver datos de la institucion propia.

El usuario institucional no debe ver selector global si pertenece a una sola institucion.

### Issuer

Color de acento: azul.

Debe emitir y consultar titulos. Los campos tecnicos como `issuer DID`, `subject DID`, hashes o referencias blockchain deben moverse a una seccion `Avanzado` cuando el flujo pueda resolverlos desde Academy/Issuer.

### Holder

Color de acento: azul o emerald para estados positivos, no como color dominante.

Debe priorizar:

- lista de credenciales,
- detalle verificable,
- estado visual,
- acciones de compartir/verificar.

### Verifier Publico

Puede usar una composicion mas centrada y amplia que los portales internos. Debe mantener claridad sobre:

- resultado,
- checks realizados,
- razon de invalidez,
- identificadores tecnicos.

## Componentes a Crear o Consolidar

| Componente | Estado | Proposito |
|------------|--------|-----------|
| `PortalShellComponent` | Existe | Layout base autenticado |
| `PortalHeaderComponent` | Pendiente | Marca, portal, usuario, logout |
| `SidButtonComponent` o directiva | Pendiente | Variantes y tamanos de botones |
| `SidFieldComponent` | Pendiente | Label, helper, error y focus comun |
| `SidPanelComponent` | Pendiente | Paneles y secciones sin repetir clases |
| `StatusBadgeComponent` | Pendiente | Estados semanticos |
| `CopyValueComponent` | Pendiente | Truncar/copiar valores tecnicos |
| `EmptyStateComponent` | Pendiente | Vacio, error y loading |

Prioridad de implementacion:

1. Extender `PortalShellComponent`.
2. Crear `SidButton`, `SidField`, `StatusBadge` y `CopyValue`.
3. Migrar Platform y Academy primero.
4. Migrar Issuer y Holder despues.
5. Corregir idioma y encoding en todo el web.

## Reglas de Calidad Visual

Antes de cerrar una pantalla:

- No debe haber mezcla innecesaria de `gray` y `slate`.
- El color naranja solo debe aparecer en acciones MetaMask.
- El color violeta solo debe aparecer en plataforma/admin global.
- Texto, botones y badges no deben romperse en mobile.
- Todo valor tecnico importante debe ser copiable o seleccionable.
- Acciones peligrosas deben tener confirmacion o friccion suficiente.
- Las consultas de datos deben tener loading, empty y error state.
- Las acciones bloqueadas por rol no deben mostrarse o deben quedar explicitamente deshabilitadas.

## Deuda UX Detectada

- Platform, Issuer y Holder aun duplican parte del shell visual.
- Issuer y Holder mezclan idioma ingles con portales que deberian estar en espanol.
- Hay textos con problemas de encoding como `sesiÃ³n`, `instituciÃ³n` o `vÃ¡lida`.
- Faltan componentes compartidos para botones, campos, badges y valores copiables.
- Issuer expone demasiados campos tecnicos en el flujo principal.
- El cliente generado del BFF debe mantenerse alineado con los endpoints nuevos.

## Definicion de Listo para Nuevas Pantallas

Una pantalla nueva de SovereignID queda lista si:

- usa `PortalShellComponent` cuando requiere sesion,
- respeta la paleta y jerarquia tipografica de este documento,
- tiene estados de carga, vacio, error y exito,
- diferencia acciones normales de acciones MetaMask,
- respeta permisos por rol,
- no duplica patrones que ya existen en `shared/ui`,
- compila con `npm run build`.
