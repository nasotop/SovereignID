## 1. Contrato y esquema de datos (fase 0)

- [x] 1.1 Extender `CredentialReadModel` (`Verifier.Application`) con `InstitutionId`, `IssuerWalletAddress`, `SubjectWalletAddress`, `Eip712Signature`, `IpfsGatewayUrl`.
- [x] 1.2 Extender `EfCredentialReadStore` con los joins necesarios (`institutions.issuer_wallet_address`, `student_wallets.wallet_address`, `credentials.eip712_signature`, `credentials.ipfs_gateway_url`).
- [x] 1.3 Añadir migración SQL: columnas `signature_validation_source` y `revocation_source` en `verification_logs`.
- [x] 1.4 Extender `VerificationChecks` (`Verifier.Domain`) con `ValidationSource` y `RevocationSource`.
- [x] 1.5 Extender `VerificationLogEntry`/`IVerificationLogStore` para persistir los nuevos campos.
- [x] 1.6 Añadir `integrity_failed` a `VerificationResult` (`Verifier.Domain`) y a su mapeo de wire (`ToWireValue`).
- [x] 1.7 Anotar `result` como `enum` de 5 valores en el documento OpenAPI de `Verifier.Api` (schema transformer existente) y reexportar `docs/contracts/verifier.openapi.json`.
- [x] 1.8 Añadir `validationSource`/`revocationSource` al esquema `VerificationChecks` del OpenAPI publicado y a `VerificationContracts.cs`.
- [x] 1.9 Añadir el código `429` con Problem Details (`error: rate_limit_exceeded`) al documento OpenAPI.

## 2. Configuración (`VerifierOptions.Evidence`)

- [x] 2.1 Crear `EvidenceVerificationOptions` con `OnChainCheckEnabled`, `IpfsCheckEnabled`, `SignatureCheckEnabled` (todos `false` por defecto) y parámetros de conexión (`RpcUrl`, `RegistryAddress`, `ChainId`, `IpfsTimeout`).
- [x] 2.2 Extender `VerifierOptions` para incluir `EvidenceVerificationOptions`.
- [x] 2.3 Documentar las variables de entorno/config nuevas en `.env.example` o equivalente del servicio verifier.

## 3. Módulo profundo `ICredentialEvidenceVerifier`

- [x] 3.1 Definir `CredentialEvidence` (record de entrada) y `CredentialEvidenceChecks` (record de salida) en `Verifier.Application`.
- [x] 3.2 Definir la interfaz `ICredentialEvidenceVerifier` con un único método `VerifyAsync(CredentialEvidence, CancellationToken)`.
- [x] 3.3 Implementar `CredentialEvidenceVerifier` como orquestador interno (ejecuta los tres chequeos en paralelo, aplica reglas de precedencia y fallback entre ellos).
- [x] 3.4 Definir seams internos privados a la implementación: `ICredentialRegistryReader`, `IIpfsContentReader`, `IEip712IssuanceSignatureVerifier`.

## 4. Chequeo on-chain (fase 1)

- [x] 4.1 Implementar utilidades `GuidToBytes32` (equivalente exacto a la del frontend) dentro de `Verifier.Infrastructure`.
- [x] 4.2 Implementar `RpcCredentialRegistryReader` (`eth_call` a `getCredential`, decodificación ABI de la tupla de retorno).
- [x] 4.3 Implementar comparación de coherencia (`contentHash`, `ipfsCid`, `institutionId`, `issuer`, `subject`) para `onChainExists`.
- [x] 4.4 Exponer `onChainRevoked` desde el mismo record leído, para alimentar `revocationSource`.
- [x] 4.5 Implementar `InMemoryCredentialRegistryReader` (fake de test) con escenarios: coherente, incoherente, no existe, timeout simulado.
- [x] 4.6 Wiring condicional en DI: `OnChainCheckEnabled=false` ⇒ no registrar/usar `RpcCredentialRegistryReader` (chequeo queda `null`).
- [x] 4.7 Tests del módulo profundo para `onChainExists` y `revocationSource` (fuente `on_chain`, `bd`, `both`) usando el fake in-memory.

## 5. Chequeo IPFS (fase 2)

- [x] 5.1 Implementar `HttpIpfsContentReader` (descarga de `ipfs_gateway_url`, timeout configurable, SHA-256 sobre bytes exactos de la respuesta).
- [x] 5.2 Implementar `InMemoryIpfsContentReader` (fake de test) con escenarios: coincide, no coincide, timeout simulado.
- [x] 5.3 Wiring condicional en DI: `IpfsCheckEnabled=false` ⇒ chequeo queda `null` sin solicitud HTTP.
- [x] 5.4 Tests del módulo profundo para `hashMatches` con el fake in-memory.

## 6. Chequeo de firma EIP-712 (fase 3)

- [x] 6.1 Añadir dependencia `Nethereum.Signer` (u equivalente) a `Verifier.Infrastructure`.
- [x] 6.2 Definir el dominio EIP-712 (`SovereignID`/`1`) y el schema tipado `CredentialIssuance`, exactamente alineados con `credential-contract.service.ts` del frontend.
- [x] 6.3 Implementar `NethereumEip712IssuanceSignatureVerifier.RecoverSigner(...)`.
- [x] 6.4 Implementar la jerarquía de fuente en `CredentialEvidenceVerifier`: usar `issuer` on-chain solo si `onChainExists=true`; si no, fallback asimétrico a `institutions.issuer_wallet_address` (coincide ⇒ `null`/`bd_fallback_inconclusive`; no coincide ⇒ `false`/`bd_fallback_rejected`; sin dato en BD ⇒ `null`/`not_evaluated`).
- [x] 6.5 Manejar firma ausente/malformada como `false` directo (dato corrupto, no infraestructura).
- [x] 6.6 Implementar `InMemoryEip712Verifier` (fake de test) parametrizable por wallet esperada/firma válida.
- [x] 6.7 Wiring condicional en DI: `SignatureCheckEnabled=false` ⇒ chequeo queda `null` sin verificación criptográfica.
- [x] 6.8 Tests del módulo profundo para los 6 escenarios de `signatureValid`/`validationSource` descritos en `specs/verifier-evidence-verification/spec.md`.

## 7. Orquestación en `VerifyCredentialUseCase`

- [x] 7.1 Integrar `ICredentialEvidenceVerifier` en `VerifyCredentialUseCase`, invocándolo solo cuando la credencial existe.
- [x] 7.2 Implementar la precedencia extendida (`not_found > revoked > expired > integrity_failed > valid`) combinando el veredicto de BD con `CredentialEvidenceChecks`.
- [x] 7.3 Poblar `VerificationChecks` con los 8 campos (`found`, `notRevoked`, `notExpired`, `hashMatches`, `onChainExists`, `signatureValid`, `validationSource`, `revocationSource`).
- [x] 7.4 Persistir el registro completo en `verification_logs` con todos los campos nuevos.
- [x] 7.5 Tests de integración end-to-end del use case cubriendo cada escenario de `specs/verifier-credential-verification/spec.md` (precedencia) y `specs/verifier-evidence-verification/spec.md`.

## 8. Rate limiting y resolución de IP (fase 4)

- [x] 8.1 Configurar `ForwardedHeadersMiddleware` en `Verifier.Api` con `KnownProxies`/`KnownNetworks` restringidos a la red interna (nginx + `bff-api`).
- [x] 8.2 Configurar `Microsoft.AspNetCore.RateLimiting` con política token bucket (capacidad 10, recarga 1 token/3s) por IP sobre `POST /verifications`.
- [x] 8.3 Mapear el rechazo por límite excedido a `429` con Problem Details (`error: rate_limit_exceeded`), coherente con `AuthFailureExceptionFilter`/ADR-0001.
- [x] 8.4 Tests de integración: ráfaga que excede capacidad, recarga tras espera, IP resuelta correctamente detrás de proxy interno simulado, header forwarded no confiable ignorado.

## 9. Contrato y verificación final

- [x] 9.1 Regenerar y commitear `docs/contracts/verifier.openapi.json` con el estado final (enum de 5 valores, `checks` extendido, `429`).
- [x] 9.2 Ejecutar el job `verify-openapi`/equivalente de CI para confirmar que no hay drift entre el snapshot y el documento generado.
- [x] 9.3 Actualizar `CONTEXT.md` (sección "Servicio `verifier`") con el nuevo comportamiento del veredicto escalonado y las fuentes de evidencia.
- [x] 9.4 Crear `docs/verifier-backend-contract.md` (referenciado desde `CONTEXT.md` pero inexistente) documentando reglas de veredicto, precedencia, fuentes de fallback y catálogo de errores.

## 10. Documentación de trabajo futuro (fuera de alcance)

- [x] 10.1 Anotar en un backlog o issue la alerta de drift institucional (`issuer_wallet_address` BD vs. `institutionIssuers` on-chain) como candidato a un cambio OpenSpec futuro.
- [x] 10.2 Anotar el rediseño de UX del portal verifier (badge `integrity_failed`, presentación de `validationSource`/`revocationSource`) como cambio de frontend posterior.
