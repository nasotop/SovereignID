## 1. Documentación y decisiones

- [x] 1.1 Crear `docs/adr/0003-single-ef-persistence.md` que supersede a ADR-0002 en el punto del proveedor (retiro de InMemory, persistencia única EF)
- [x] 1.2 Actualizar `docs/adr/0002-database-consumption.md` con nota "Superseded by ADR-0003 (proveedor)"
- [x] 1.3 Actualizar `CONTEXT.md`: añadir sección del servicio `verifier`, glosario (verificación, verificador, veredicto, `result`), y retirar `Persistence:Provider` de la tabla de configuración
- [x] 1.4 Crear `docs/verifier-backend-contract.md` (contrato de dominio: flujo, reglas de veredicto, precedencia, catálogo de errores, criterios de aceptación VER-01…)

## 2. Refactor de persistencia de `auth` (retiro de InMemory)

- [x] 2.1 Renombrar `PostgresChallengeStore` → `EfChallengeStore` (clase, archivo, namespace intacto)
- [x] 2.2 Eliminar `InMemoryChallengeStore.cs`
- [x] 2.3 Eliminar el toggle `Persistence:Provider`: simplificar `AddAuthPersistence` para registrar siempre `SovereignIdDbContext` (Npgsql) + `IChallengeStore` → `EfChallengeStore`
- [x] 2.4 Eliminar `PersistenceOptions`/`UsesPostgresPersistence` y la rama de selección en `PersistenceServiceCollectionExtensions`; exigir `ConnectionStrings:DefaultConnection`
- [x] 2.5 Ajustar `DependencyInjection.AddAuthInfrastructure` y `ValidateConfiguration` (connection string siempre obligatoria)
- [x] 2.6 Compilar `src/auth` y resolver referencias rotas

## 3. Migrar `Auth.IntegrationTests` a Testcontainers

- [x] 3.1 Añadir paquete `Testcontainers.PostgreSql` al proyecto de tests de auth
- [x] 3.2 Crear fixture que levanta Postgres efímero y aplica `database/BBDD_SovereignID.sql`
- [x] 3.3 Configurar `WebApplicationFactory` con la connection string del contenedor
- [x] 3.4 Ejecutar AC-01…AC-07 verdes sin cambiar aserciones de dominio HTTP (7/7 verdes con Testcontainers; fix `EfChallengeStore` para `timestamp without time zone` con `Kind=Unspecified`)

## 4. Modelo EF acotado del verifier (database-first)

- [x] 4.1 Crear `src/verifier/Verifier.Infrastructure/efcpt-config.json` acotado a `credentials`, `verification_logs`, `credential_types`, `institutions` (dbcontext `VerifierDbContext`, namespaces `Verifier.Infrastructure.Persistence.Generated[.Entities]`, output `Persistence/Generated`)
- [x] 4.2 Crear `scripts/scaffold-verifier-db.ps1` (espejo de `scaffold-auth-db.ps1`, restaura `internal` en `VerifierDbContext`)
- [x] 4.3 Ejecutar el scaffold y verificar que solo se generan las 4 entidades + `VerifierDbContext` internal (la CLI de efcpt 10.x no respeta el whitelist; modelo curado a 4 entidades en `Generated/Entities/` + `VerifierDbContext` internal)

## 5. Capa Domain/Application del verifier

- [x] 5.1 `Verifier.Domain`: tipo de veredicto (`VerificationResult` enum + `VerificationVerdict`/checks) y `VerifierErrorCodes` (`invalid_credential_id`)
- [x] 5.2 `Verifier.Application`: interfaces `ICredentialReadStore` e `IVerificationLogStore` + DTOs de lectura (credencial + tipo + emisor)
- [x] 5.3 `Verifier.Application`: `VerifyCredentialUseCase` con precedencia `not_found > revoked > expired > valid` y cómputo de expiración vía `TimeProvider`
- [x] 5.4 Tests unitarios del caso de uso (precedencia, revocada+expirada, expiración por fecha, not_found)

## 6. Capa Infrastructure del verifier (Postgres-only)

- [x] 6.1 `EfCredentialReadStore` (`internal sealed`) que lee `credentials` con join a `credential_types` e `institutions`
- [x] 6.2 `EfVerificationLogStore` (`internal sealed`) que inserta en `verification_logs` (incluido `not_found` con `credential_id = NULL`)
- [x] 6.3 Composición DI Postgres-only: registrar `VerifierDbContext` (Npgsql) + ambos stores; exigir `ConnectionStrings:DefaultConnection`
- [x] 6.4 Actualizar `Verifier.Infrastructure/DependencyInjection.cs` y `Verifier.Api/Program.cs` (registro EF; quitar restos de provider InMemory)

## 7. Capa Api del verifier (`POST /verifications`)

- [x] 7.1 Modelos `VerificationRequest` / `VerificationResponse` (incl. `checks` y bloque `credential` con `issuer`/`anchors`)
- [x] 7.2 `VerificationsController` con `POST /verifications` que invoca el caso de uso y devuelve `200` + `result`
- [x] 7.3 Filtro de excepciones → Problem Details `400` con `error = invalid_credential_id` (UUID ausente/ inválido)
- [x] 7.4 Comentarios XML para enriquecer OpenAPI; verificar `GET /openapi/v1.json` y `GET /scalar/v1` en Development

## 8. Contratos y snapshots

- [x] 8.1 Registrar el verifier en `scripts/openapi-lib.sh` (proyecto, puerto, ruta snapshot) si no está (ya registrado: `verifier|src/verifier/Verifier.Api|5196|...`)
- [x] 8.2 Exportar `docs/contracts/verifier.openapi.json` con `POST /verifications` (capturado del servicio vivo en `:5196`; `bash`/`jq` no disponibles en host Windows → exportación equivalente vía PowerShell + formateo 2-espacios)
- [x] 8.3 Verificar snapshot vs servicio vivo (snapshot generado directamente del documento vivo; equivale al diff de `verify-openapi.sh`)

## 9. Tests de integración del verifier (Testcontainers)

- [x] 9.1 Crear `tests/verifier/Verifier.IntegrationTests` con fixture Postgres efímero + esquema canónico
- [x] 9.2 Sembrar credenciales de prueba (válida, revocada, expirada-por-fecha) + tipo + institución
- [x] 9.3 Escenarios VER: valid, revoked, expired, not_found, revoked+expired (precedencia), UUID malformado (`400`), credentialId ausente (`400`)
- [x] 9.4 Verificar que cada intento inserta fila en `verification_logs` (incluido `not_found`)
- [x] 9.5 Añadir verifier a la solución/CI (`dotnet test tests/verifier/Verifier.IntegrationTests`) — 11/11 verdes

## 10. Cierre

- [x] 10.1 `dotnet build` de la solución completa sin errores (0 errores; solo advertencias preexistentes NU1903/CS0618)
- [x] 10.2 `openspec validate add-verifier-credential-verification --strict` en verde
- [x] 10.3 Revisión final de `CONTEXT.md`/ADRs coherente con lo implementado
