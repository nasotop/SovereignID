# Trabajo futuro — add-verifier-evidence-checks

Registro de ítems explícitamente fuera de alcance del cambio `add-verifier-evidence-checks`.

## Alerta de drift institucional

**Candidato a cambio OpenSpec futuro:** reconciliar proactivamente `institutions.issuer_wallet_address` (BD) con `institutionIssuers` on-chain del contrato `CredentialRegistry`. Naturaleza institucional/proactiva, distinta a la verificación reactiva por credencial implementada aquí.

## UX del portal verifier

**Completado** en el cambio `frontend-onchain-verification-ui`: badge `integrity_failed`, checks agrupados, fuentes `validationSource`/`revocationSource`, banner de evidencia deshabilitada, anclas compartidas (`CredentialAnchorsPanel`) y ciclo holder → verifier vía query param.
