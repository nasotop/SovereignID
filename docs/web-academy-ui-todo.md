# Web Academy UI TODO

Este TODO mapea el trabajo necesario para que el proyecto Angular cubra los roles y flujos del microservicio `academy`.

## 1. Auth, roles y routing

- [x] Limpiar rutas duplicadas en `src/web/src/app/app.routes.ts`.
- [x] Ajustar redireccion post-login por prioridad:
  - `platform_admin` -> `/platform`
  - `admin` institucional -> `/academy`
  - `issuer` -> `/issuer`
  - `viewer` -> `/academy`
  - `holder` -> `/holder`
- [ ] Soportar usuarios multirol con un destino razonable o selector de portal.
- [x] Asegurar que `roleGuard` distinga bien platform admin, holder y membresias institucionales.

## 2. Contratos y clientes

- [x] Exponer en BFF los endpoints nuevos de `academy`.
- [ ] Regenerar snapshot `docs/contracts/bff.openapi.json`.
- [ ] Regenerar cliente Angular `src/web/src/app/api/bff`.
- [x] Extender `AcademyService` Angular con los endpoints nuevos.

## 3. Sistema UI base

- [x] Documentar sistema de diseno web, paleta, tipografia, layout y reglas UX.
- [x] Documentar patron maestro-detalle para vistas internas y modales para formularios.
- [x] Crear shell reutilizable para portales.
- [ ] Crear header reutilizable con marca, nombre de portal y logout.
- [ ] Crear componentes/directivas base para botones, campos, badges y valores copiables.
- [x] Crear `StatusBadgeComponent` para estados semanticos.
- [x] Crear `CopyValueComponent` para UUID, DID y wallets copiables.
- [x] Agregar feedback visible de carga, exito y error en acciones Platform/Academy.
- [ ] Reducir duplicacion de clases Tailwind en Platform, Academy, Holder e Issuer.

## 4. Platform Portal (`platform_admin`)

- [x] Refactorizar Platform a layout maestro-detalle: listado izquierdo y panel derecho de KPIs/detalle.
- [x] Mover formulario de crear institucion a modal/drawer.
- [x] Mover consulta/invitacion de institucion a acciones contextuales o modales.
- [x] Listar instituciones.
- [x] Crear institucion.
- [x] Ver detalle de institucion.
- [x] Invitar usuarios institucionales.
- [ ] Cambiar rol institucional.
- [ ] Revocar usuario institucional.
- [x] Entrar a la administracion Academy de una institucion.

## 5. Academy Portal (`admin`, `issuer`, `viewer`)

- [x] Crear ruta `/academy`.
- [x] Refactorizar Academy a layout maestro-detalle para estudiantes y usuarios.
- [x] Mover formularios de crear estudiante, vincular wallet e invitar usuario a modales.
- [ ] Seleccionar institucion si el usuario pertenece a mas de una.
- [x] Mostrar resumen de institucion.
- [x] Listar estudiantes.
- [x] Crear estudiante.
- [x] Vincular wallet manual a estudiante.
- [x] Listar usuarios institucionales.
- [x] Invitar usuario institucional.
- [x] Cambiar/revocar roles institucionales.
- [x] Ocultar o bloquear acciones segun permisos:
  - `admin`: gestiona estudiantes, usuarios y carreras.
  - `issuer`: lectura operativa para emitir, sin administrar usuarios.
  - `viewer`: solo lectura.

## 6. UX y contenido

- [ ] Unificar idioma de portales institucionales en espanol.
- [ ] Reservar naranja solo para acciones MetaMask.
- [ ] Reservar violeta para plataforma.
- [ ] Usar azul como accion primaria general.
- [ ] Tratar wallet, DID, UUID y hashes como valores tecnicos copiables.
- [ ] Mover campos tecnicos de Issuer a una seccion avanzada cuando sea posible.

## 7. Pruebas

- [ ] Tests unitarios de `AuthService.getDefaultPortalUrl`.
- [ ] Tests de `roleGuard`.
- [ ] Tests de `AcademyService`.
- [ ] Tests de visibilidad de acciones por rol en Platform/Academy.
- [x] `npm run build`.
- [x] `npm test` si el entorno lo permite.
