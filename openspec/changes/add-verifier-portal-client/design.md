## Context

El backend `verifier` ya implementa `POST /verifications` (veredicto escalonado v1, Problem Details para `invalid_credential_id`) y publica su contrato en `docs/contracts/verifier.openapi.json`. El portal web, en cambio, es una plantilla estática (`VerifierComponent {}`): sin servicio, sin modelos, sin `HttpClient`. El servicio `auth` ya estableció el patrón de comunicación del front (servicio HTTP + seam de errores `error.utils.ts` + URLs relativas proxeadas por nginx), pero con **tipos escritos a mano**.

Restricciones vigentes que este diseño debe respetar:
- Seam único de Problem Details en `error.utils.ts`; los componentes no leen el cuerpo HTTP directamente ([ADR-0001](../../../docs/adr/0001-problem-details-errors.md)).
- El snapshot OpenAPI versionado es la fuente de verdad del contrato HTTP; CI lo verifica.
- Angular 22 (standalone, signals, zoneless), Tailwind v4, filosofía de dependencias mínimas.

## Goals / Non-Goals

**Goals:**
- Que el portal verifier llame realmente a `POST /verifications` y muestre el veredicto.
- Eliminar el *drift* tipo manual ↔ contrato generando el cliente desde el OpenAPI.
- Mantener intacto el seam de Problem Details pese a usar un cliente generado.
- Dejar el verifier alcanzable de punta a punta (gateway + stack docker).

**Non-Goals:**
- Escáner QR (futuro; el QR codifica el UUID en crudo, decisión ya tomada).
- Migrar `auth` al codegen (se hará en un cambio posterior; conviven dos estilos).
- Implementar los chequeos externos (`hashMatches`, `onChainExists`, `signatureValid`): siguen `null` en v1.
- Rate limiting / anti-abuso del endpoint anónimo.

## Decisions

### D1: Entrada por UUID en campo de texto (no archivo, no QR aún)
La UI de drag & drop de `.json` contradice el contrato v1 (entrada = UUID, VC completa excluida). Se reemplaza por un input de texto de `credentialId` con validación de formato UUID en cliente.
**Alternativas:** (a) mantener subida de archivo y extraer el UUID en cliente — reintroduce parsing de VC fuera de alcance; (b) QR ahora — requiere cámara/permiso/librería, se difiere.

### D2: Cliente HTTP generado con `ng-openapi-gen` (revierte "sin codegen")
Modelos + servicio del front se generan desde `docs/contracts/verifier.openapi.json`. Salida en `src/web/src/app/api/verifier/`, `rootUrl: ''` (URLs relativas como `/verifications`, proxeadas por nginx), generación vía `npm run gen:api:verifier` con el **código generado commiteado**. Decisión formal en [ADR-0004](../../../docs/adr/0004-openapi-client-codegen.md).
**Alternativas:** (a) seguir a mano como auth — duplica el contrato, abre drift, es justo el síntoma a corregir; (b) generar en build/CI sin commitear — el build pasa a depender del codegen; se prefiere el espejo de "snapshot commiteado".

### D3: Fachada `VerifierService` a mano sobre el servicio generado
El servicio generado lanza `HttpErrorResponse` crudo; el componente no puede tocarlo (regla del seam). Una fachada delgada inyecta el `VerificationsService` generado, valida el UUID, ejecuta la llamada y traduce errores con `error.utils.ts` (`toThrownError`/`toErrorCode`). El componente solo habla con la fachada — análogo a `auth.service.ts` sobre `auth-api.service.ts`.
**Alternativa:** interceptor HTTP global de Problem Details — cambio transversal mayor, innecesario para una sola ruta.

### D4: `result` como `enum` en el OpenAPI (enriquecer el contrato)
Hoy el backend serializa `result` como `string` libre (`ToWireValue`), así que el OpenAPI no lleva `enum` y el codegen produciría `result: string`. Se anota `result` como `enum` (`valid|revoked|expired|not_found`) en el documento OpenAPI vía schema transformer en `Verifier.Api` (o un enum con `JsonConverter` snake_case), **sin** cambiar el valor en el wire ni `ToWireValue`. Tras el cambio se reexporta el snapshot.
**Alternativa:** narrowear con un type union a mano en el front — reintroduce un artefacto manual que contradice D2.

### D5: Transporte vía gateway nginx + servicio `verifier-api` en compose
El front llama a la ruta relativa `/verifications`; se añade `location /verifications` en `nginx.conf` apuntando a `http://verifier-api:8080/verifications`, y un servicio `verifier-api` en `docker-compose.yml` (Dockerfile ya existe) en `sovereign-net`, con `ConnectionStrings__DefaultConnection` y `depends_on: postgres healthy`. Mismo modelo de gateway que `auth`.
**Alternativa:** apuntar el front a un host absoluto del verifier — rompe el patrón relativo + proxy y complica entornos.

## Risks / Trade-offs

- [Dos estilos de cliente conviven (auth manual, verifier generado)] → Documentado en ADR-0004 y CONTEXT.md; migración de auth planificada como cambio posterior.
- [El schema transformer de enum podría desalinear el snapshot si no se reexporta] → La tarea incluye reexportar y el job CI `verify-openapi` falla si el snapshot quedó desactualizado.
- [Código generado commiteado puede quedar obsoleto frente al snapshot] → Script `gen:api:verifier` reproducible; CI puede regenerar y diffear para detectar drift.
- [`BarcodeDetector`/QR diferido deja una expectativa de UX] → Alcance explícito; el campo UUID es funcional por sí solo y el QR rellenará ese mismo campo cuando se añada.
- [El verifier en compose requiere connection string válida] → Coherente con ADR-0003 (persistencia única); se documenta en `.env.example`.

## Migration Plan

1. Enriquecer `result` como enum en `Verifier.Api` y reexportar `docs/contracts/verifier.openapi.json`.
2. Añadir `ng-openapi-gen` + config + script; generar y commitear `src/app/api/verifier/`.
3. Implementar fachada `VerifierService` y reescribir `VerifierComponent`.
4. Añadir `location /verifications` en nginx y el servicio `verifier-api` en compose.
5. Verificación e2e con `docker compose up` (web + verifier-api + postgres) y datos de prueba.

Rollback: revertir el commit; el portal vuelve a su estado estático sin afectar a `auth` ni al backend del verifier.

## Open Questions

- Ninguna bloqueante. (Pendiente futuro: escáner QR y migración de `auth` al codegen, ambos fuera de alcance.)
