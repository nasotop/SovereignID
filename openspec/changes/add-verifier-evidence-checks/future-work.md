# Trabajo futuro — add-verifier-evidence-checks

Registro de ítems explícitamente fuera de alcance del cambio `add-verifier-evidence-checks`.

## Alerta de drift institucional

**Candidato a cambio OpenSpec futuro:** reconciliar proactivamente `institutions.issuer_wallet_address` (BD) con `institutionIssuers` on-chain del contrato `CredentialRegistry`. Naturaleza institucional/proactiva, distinta a la verificación reactiva por credencial implementada aquí.

## UX del portal verifier

**Cambio de frontend posterior:** presentar el badge `integrity_failed` y exponer `validationSource` / `revocationSource` en la UI del portal (cliente Angular vía BFF). El backend y el contrato OpenAPI ya soportan los campos; la regeneración mecánica del cliente no sustituye el diseño visual.
