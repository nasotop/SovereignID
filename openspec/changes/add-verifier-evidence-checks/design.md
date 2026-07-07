## Context

`Verifier.Api` expone `POST /verifications`, un endpoint público y anónimo que hoy solo resuelve el veredicto contra la base de datos propia (`found`, `notRevoked`, `notExpired`). Los tres chequeos con dependencia externa (`hashMatches`, `onChainExists`, `signatureValid`) están reservados como `null` desde v1 (ver `VerifyCredentialUseCase.cs`).

Fuentes de verdad externas disponibles:
- **Contrato `CredentialRegistry`** en Sepolia (`docs/contracts/credential-registry.sepolia.json`), con `getCredential(bytes32)` devolviendo `contentHash, ipfsCid, institutionId, issuer, subject, revoked`.
- **IPFS**, vía `ipfs_gateway_url` persistido por credencial (columna `credentials.ipfs_gateway_url`).
- **Firma EIP-712** `CredentialIssuance` (dominio `SovereignID`/`1`), generada en el frontend al emitir (`credential-contract.service.ts`) y persistida en `credentials.eip712_signature`.

`Issuer.Api` ya verifica anclas on-chain al emitir/revocar (`RpcBlockchainAnchorVerifier`), pero solo mira el receipt de la transacción (emisor, bloque) — **no** valida la firma EIP-712 criptográficamente ni llama a `getCredential`. Este cambio no toca `Issuer`; es la primera vez que el backend hace `ecrecover` real y lectura de estado del contrato.

Restricciones vigentes:
- El endpoint es público (sin JWT); cualquier chequeo nuevo que dispare I/O externo por request es también una nueva superficie de costo/abuso.
- `docs/verifier-backend-contract.md`, referenciado en `CONTEXT.md`, no existe todavía — este cambio es la oportunidad de crearlo.
- Patrón ya establecido en `Issuer` para degradación graceful: `Configurable*Verifier` que resuelve a un adapter nulo si la config no está habilitada (`ConfigurableBlockchainAnchorVerifier`).

Estas decisiones surgieron de una sesión de grilling explícita centrada en cerrar ambigüedades de seguridad antes de escribir código; el punto de partida crítico fue: **un fallback a BD para `signatureValid` es tan confiable como la propia BD**, y BD puede estar comprometida o desincronizada — por lo que cualquier mecanismo de "respaldo" debe ser incapaz de generar una confirmación falsa.

## Goals / Non-Goals

**Goals:**
- Implementar `onChainExists`, `hashMatches` y `signatureValid` contra sus fuentes reales, con fallback seguro cuando la infraestructura externa no responde.
- Garantizar que ningún camino de fallback pueda **confirmar** (`true`) algo que solo la fuente potencialmente comprometida (BD) afirma — el fallback solo puede rechazar o quedar inconcluso.
- Extender el veredicto (`result`) con `integrity_failed` sin romper el modelo de precedencia existente.
- Proteger el endpoint público del costo/abuso que introducen las llamadas RPC/IPFS por request.
- Permitir activación incremental por fases (on-chain → IPFS → firma) vía configuración, sin re-deploys de código entre fases.

**Non-Goals:**
- Cambios de UI/UX en el portal Angular del verifier — cambio backend-only; el rediseño del componente queda para un cambio posterior.
- Alerta de drift institucional (`issuer_wallet_address` BD vs. `institutionIssuers` on-chain) — es un problema proactivo/institucional distinto al reactivo/por-credencial que resuelve este cambio; se documenta como trabajo futuro.
- Modo estricto "exigir on-chain, rechazar fallback" configurable por tipo de credencial — no hay caso de uso confirmado hoy (los 4 tipos de credencial del MVP son homogéneos en criticidad).
- Extraer un paquete común compartido con `Issuer` (`SovereignID.CredentialAnchoring`) — solo `Verifier` es consumidor real hoy; extraer sin un segundo consumidor real es especular sobre reutilización.
- Selección de gateway IPFS con fallback a múltiples proveedores — se usa el único gateway ya persistido por credencial.
- QR / URL navegable en el portal verifier — ya diferido en `add-verifier-portal-client`, sin cambios aquí.

## Decisions

### D1: Módulo profundo `ICredentialEvidenceVerifier` en vez de puertos sueltos por cada dependencia externa

**Decisión:** Introducir una única interfaz de aplicación, `ICredentialEvidenceVerifier.VerifyAsync(CredentialEvidence) → CredentialEvidenceChecks`, que internamente orquesta RPC, IPFS y verificación EIP-712 mediante adapters privados a su implementación. `VerifyCredentialUseCase` solo conoce esta interfaz — no RPC, no IPFS, no criptografía.

**Alternativa rechazada:** exponer `IIpfsFetcher`, `IHashComputer`, `IRegistryReader`, `IEip712Verifier` directamente al use case. Esto convierte al use case en orquestador + experto en red + experto en criptografía a la vez, y "borrar" el use case no haría desaparecer la complejidad (reaparecería en el controller) — falla el *deletion test*. Un módulo profundo concentra esa complejidad detrás de una interfaz pequeña.

**Adapters internos** (privados a la implementación, con 2 adapters reales cada uno — producción + fake de test, justificando el seam):

| Seam interno | Adapter producción | Adapter test |
|---|---|---|
| `ICredentialRegistryReader` | `RpcCredentialRegistryReader` (`eth_call`) | `InMemoryCredentialRegistryReader` |
| `IIpfsContentReader` | `HttpIpfsContentReader` | `InMemoryIpfsContentReader` |
| `IEip712IssuanceSignatureVerifier` | `NethereumEip712IssuanceSignatureVerifier` | `InMemoryEip712Verifier` |

### D2: `integrity_failed` como quinto valor de `result` (no una interpretación silenciosa de `valid`)

**Decisión:** `result` gana `integrity_failed`. Precedencia: `not_found > revoked > expired > integrity_failed > valid`. Se dispara solo si algún check **evaluado** (no `null`) devuelve `false`.

**Alternativas rechazadas:**
- Mantener `result=valid` con checks en `false` por separado — un verificador que solo lee el badge (`result`) nunca vería la alerta; es un veredicto ambiguo por diseño.
- Reusar `not_found` para credenciales con integridad rota — semánticamente incorrecto (la credencial existe en BD, el problema es de evidencia externa) y rompería la trazabilidad en `verification_logs` (`credential_id` quedaría `NULL` cuando sí hay credencial).

**Trade-off aceptado:** cambio de contrato (`BREAKING` a nivel de enum) — el cliente Angular generado debe regenerarse; el manejo visual del nuevo valor queda fuera de alcance (Non-Goal).

### D3: Revocación — on-chain puede elevar, BD nunca se "des-revoca"

**Decisión:** `notRevoked = !(bdRevoked || onChainRevoked)`. Si `onChainRevoked=true`, `result=revoked` aunque BD diga `active`. Si `bdRevoked=true`, `result=revoked` aunque on-chain no lo refleje (aún) — nunca se prioriza on-chain para *negar* una revocación ya registrada en BD.

Nuevo campo `revocationSource` ∈ `{bd, on_chain, both}` en `checks` y `verification_logs`, para auditar desincronizaciones operativas (revocado on-chain, backoffice institucional no actualizado).

**Alternativa rechazada:** "BD gana siempre" — dejaría pasar como `valid` una credencial que la propia cadena (fuente de ancla) ya marcó revocada, contradiciendo la premisa del producto ("blockchain como ancla de confianza").

### D4: `onChainExists` exige coherencia completa, no solo existencia

**Decisión:** `onChainExists=true` solo si el registro existe **y** `contentHash`, `ipfsCid`, `institutionId`, `issuer`, `subject` coinciden exactamente con BD (case-insensitive en direcciones). Un registro que existe pero es incoherente cuenta como `onChainExists=false`.

**Alternativa rechazada:** `onChainExists=true` por sola existencia, dejando el mismatch de campos solo a `hashMatches`. Produciría un `onChainExists=true` con datos contradictorios — confuso para el validador y, más importante, abriría la puerta a que otros checks (ver D5) confiaran selectivamente en campos de un registro ya sabido inconsistente.

### D5: Firma EIP-712 — jerarquía estricta con fallback asimétrico

Esta es la decisión de mayor peso en seguridad de todo el cambio, resultado directo de identificar en el grilling que un fallback simétrico a BD es explotable: si un atacante compromete `institutions.issuer_wallet_address` y produce una firma válida contra esa wallet, un fallback simétrico (BD confirma `true` cuando RPC cae) confirmaría criptográficamente un dato falso sin que la cadena lo haya avalado nunca.

**Decisión:**
1. Si `onChainExists=true` (registro existe y es coherente): el `issuer` on-chain es la única fuente para `signatureValid`. `true`/`false` según `ecrecover`.
2. Si el registro on-chain existe pero es **incoherente** (`onChainExists=false` por mismatch): su `issuer` **no se usa** — se trata como si on-chain no estuviera disponible para este propósito (no se "cherry-pickea" un campo de un registro ya desconfiado).
3. Si on-chain no está disponible (RPC caído, o incoherente por el punto 2): fallback **asimétrico** a `institutions.issuer_wallet_address` (BD):
   - Firma coincide con BD → `signatureValid=null`, `validationSource=bd_fallback_inconclusive` (**no confirma**).
   - Firma no coincide ni con BD → `signatureValid=false`, `validationSource=bd_fallback_rejected` (rechazo válido incluso sin cadena).
   - BD tampoco tiene wallet configurada → `signatureValid=null`, `validationSource=not_evaluated`.
4. Firma ausente o malformada en BD (dato corrupto, no problema de infraestructura) → `signatureValid=false` directamente, con `validationSource` reflejando la fuente que se intentó usar.

**Alternativas consideradas y rechazadas:**
- **Fallback simétrico** (BD puede confirmar `true`): vector de riesgo descrito arriba.
- **`null` puro sin fallback cuando RPC cae**: pierde la capacidad de **detectar** firmas obviamente inválidas durante un corte de RPC (el fallback asimétrico conserva esa detección sin poder confirmar falsamente).
- **BD como fuente primaria, on-chain como refuerzo**: invierte la jerarquía de confianza; la cadena es el ancla, no el índice.

Campo `validationSource` ∈ `{on_chain, bd_fallback_inconclusive, bd_fallback_rejected, not_evaluated}` expuesto en `checks` (no solo en logs), porque un validador externo con menor garantía criptográfica en su verificación tiene derecho a saberlo (regla explícita: "nunca default silencioso").

### D6: `hashMatches` — comparación de bytes exactos, sin gateway alternativo

**Decisión:** un único intento contra `ipfs_gateway_url` (ya persistido por credencial en el momento de emisión), timeout ~8s ⇒ `null` si no responde. SHA-256 se calcula sobre los bytes UTF-8 exactos devueltos por el gateway, nunca sobre un re-serialize de un objeto parseado (evita romper el match con el `JSON.stringify` no canónico usado en emisión).

**Alternativa rechazada:** lista de gateways de fallback. La elección del gateway es una decisión del **emisor** (persistida en BD), no del verificador; introducir alternativas en el verifier sin que el emisor las conozca no tiene beneficio claro y añade latencia en el peor caso.

### D7: Configuración — tres flags independientes, `false` por defecto en todo entorno

**Decisión:** `VerifierOptions.Evidence` con `OnChainCheckEnabled`, `IpfsCheckEnabled`, `SignatureCheckEnabled`, cada uno independiente y `false` por defecto (patrón ya usado en `Issuer.ConfigurableBlockchainAnchorVerifier`). Permite activar cada fase de implementación en producción de forma incremental y apagar selectivamente un check si su infraestructura externa está degradada, sin tocar código ni afectar a los otros dos.

**Alternativa rechazada:** un único switch maestro `Evidence.Enabled`. Obligaría a apagar los tres checks ante la falla de uno solo (p. ej. gateway IPFS caído), perdiendo `onChainExists`/`signatureValid` que seguirían funcionando.

### D8: Rate limiting en `Verifier.Api`, no en el BFF

**Decisión:** token bucket por IP (capacidad 10, recarga 1 token/3s) implementado con el rate limiting nativo de .NET 10 (`Microsoft.AspNetCore.RateLimiting`) directamente en `Verifier.Api`. Requiere `ForwardedHeadersMiddleware` con `KnownProxies`/`KnownNetworks` restringidos a la red interna del stack (nginx + `bff-api` en `sovereign-net`) para resolver la IP real del cliente detrás del proxy.

**Motivo del cambio de alcance respecto al diseño previo** (`add-verifier-portal-client` marcó esto como Non-Goal): en v1 el endpoint solo consultaba BD propia (barato). Con este cambio, cada verificación puede disparar una llamada RPC y una descarga IPFS — el endpoint público y anónimo pasa a tener un costo real por request, y por tanto una superficie de abuso nueva que no existía antes.

**Alternativa rechazada:** rate limiting en el BFF. El BFF es un proxy delgado (ADR-0005) sin lógica de negocio; proteger ahí no cubre a otros consumidores directos del verifier y desalinea la responsabilidad del control de costo con el servicio que efectivamente lo genera.

**Alternativa rechazada (mitigación más liviana):** cache corto de 60s por `credentialId`. Se descartó en favor de protección completa por IP, decisión explícita del usuario sobre mi recomendación inicial (cache).

### D9: Sin paquete común compartido con `Issuer`

**Decisión:** las primitivas nuevas (Guid→bytes32, SHA-256 de contenido, dominio EIP-712, ABI de `getCredential`) viven dentro de `Verifier.Application`/`Verifier.Infrastructure`.

**Alternativa rechazada:** extraer `src/common/SovereignID.CredentialAnchoring` ahora. Aplicando la regla "un adapter implica un seam hipotético, dos adapters implican uno real": hoy solo `Verifier` consume estas primitivas activamente. Si `Issuer` necesita lo mismo en un cambio futuro, se extrae entonces con dos consumidores reales delante, conociendo la forma exacta que ambos necesitan.

## Risks / Trade-offs

- **[Corte de RPC/IPFS prolongado degrada informativamente, no bloquea]** → Con `Evidence` flags activados, un corte de infraestructura externa produce `null`/fallback asimétrico en vez de bloquear el endpoint; el `result` de negocio (BD) sigue respondiendo. Mitigado por diseño (D5, D7).
- **[BD comprometida ya no puede generar falsos positivos de firma]** → Mitigado estructuralmente por el fallback asimétrico (D5): una BD comprometida puede en el peor caso producir `null` (inconcluso) o ayudar a rechazar, nunca confirmar.
- **[Latencia del endpoint público aumenta con 2 llamadas de red por request]** → Timeouts acotados (~8s IPFS) y ejecución en paralelo de los tres checks (detalle de implementación, no requiere flag); rate limiting (D8) evita que el costo se multiplique por abuso.
- **[Enum `result` es un cambio de contrato breaking]** → Aceptado explícitamente; el cliente Angular se regenera mecánicamente. El manejo visual del nuevo valor es Non-Goal de este cambio, riesgo de UX inconsistente hasta el cambio de frontend posterior.
- **[Rate limiting por IP puede afectar a verificadores legítimos detrás de NAT compartido]** → Mitigado por el uso de token bucket (tolera ráfagas) en vez de ventana fija; ajustable vía configuración si en producción se observan falsos positivos.
- **[`ForwardedHeadersMiddleware` mal configurado permitiría spoofing de IP]** → Mitigado restringiendo `KnownProxies`/`KnownNetworks` explícitamente a la red interna conocida del stack (nginx + bff-api), no aceptando el header desde cualquier origen.
- **[Drift institucional (BD vs on-chain a nivel `institutionIssuers`) no se detecta proactivamente]** → Aceptado como Non-Goal; documentado como trabajo futuro (posible job de reconciliación en `Reports.Api` o servicio dedicado).

## Migration Plan

Implementación por fases, habilitando cada `*CheckEnabled` de forma independiente en producción tras validar la fase anterior:

1. **Fase 0:** extender `CredentialReadModel` y la proyección EF (`EfCredentialReadStore`) con los campos nuevos necesarios (`InstitutionId`, `IssuerWalletAddress`, `SubjectWalletAddress`, `Eip712Signature`, `IpfsGatewayUrl`). Migración de BD: nuevas columnas `signature_validation_source`, `revocation_source` en `verification_logs`.
2. **Fase 1:** `ICredentialEvidenceVerifier` + `RpcCredentialRegistryReader` → habilita `onChainExists` y `revocationSource` (no depende de IPFS ni de la firma). Activar `OnChainCheckEnabled=true` en producción tras pruebas.
3. **Fase 2:** `HttpIpfsContentReader` → habilita `hashMatches`. Activar `IpfsCheckEnabled=true`.
4. **Fase 3:** `NethereumEip712IssuanceSignatureVerifier` con la jerarquía asimétrica completa → habilita `signatureValid` y `validationSource`. Activar `SignatureCheckEnabled=true`.
5. **Fase 4:** rate limiting (`Microsoft.AspNetCore.RateLimiting` + `ForwardedHeadersMiddleware`) — desplegar junto con o antes de la Fase 1, dado que el riesgo de costo/abuso aparece desde el primer check habilitado.
6. Reexportar `docs/contracts/verifier.openapi.json` (enum `result` + campos nuevos en `checks` + respuesta `429`) tan pronto el contrato esté cerrado (puede ir antes de las fases de activación, ya que los campos nuevos son `null`/ausentes hasta que se habilite cada flag).

**Rollback:** cada fase es reversible apagando su flag correspondiente (`*CheckEnabled=false`) sin redeploy; el rate limiting puede desactivarse revirtiendo el middleware. El cambio de enum (`integrity_failed`) es la única pieza no trivialmente reversible una vez que un cliente empieza a depender del nuevo valor — mitigado por el Non-Goal de mantener el frontend sin cambios de UX hasta un cambio posterior explícito.

## Open Questions

Ninguna bloqueante. Todas las ambigüedades de seguridad y alcance identificadas durante el diseño se resolvieron explícitamente (ver Decisions). Quedan documentados como trabajo futuro, fuera de este cambio:
- Alerta de drift institucional (`issuer_wallet_address` BD vs. `institutionIssuers` on-chain).
- Modo estricto "exigir on-chain" configurable por tipo de credencial.
- Extracción de `SovereignID.CredentialAnchoring` si `Issuer` necesita las mismas primitivas en el futuro.
- Rediseño de UX del portal verifier para los nuevos campos/veredicto.
