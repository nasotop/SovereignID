## Context

Tras `add-issuer-content-anchor`, el seam de anclaje vive en issuer-api. El frontend debe reducirse a cliente delgado:

```
VcDocumentService.buildTitleCredential()
  → POST .../documents/anchor (BFF → issuer)
  → CredentialContractService (on-chain)
  → POST .../students/{id}/title
```

Estado actual a eliminar (`ipfs-pinning.service.ts`):

- `JSON.stringify` + `sha256HexFromString` local.
- Pinata JWT desde `localStorage`.
- CID fake `bafy${hash}dev`.

## Goals / Non-Goals

**Goals:**

- Un solo camino de anclaje: backend issuer.
- Borrado total de `IpfsPinningService` y JWT Pinata en browser.
- Errores de dominio visibles en UI antes de abrir MetaMask.
- Mantener `VcDocumentService` en Angular.

**Non-Goals:**

- Cambios en verifier UI.
- Feature flag para volver al pinning legacy.
- Configurar Pinata en el frontend.

## Decisions

### D1: `IssuerApiService.anchorDocument(institutionId, document)`

**Decisión:** Método dedicado que POST al BFF con el VC JSON-LD. Retorna `ContentAnchor` tipado (`contentHash`, `ipfsCid`, `ipfsGatewayUrl`).

**Alternativa rechazada:** usar solo cliente generado sin fachada — aceptable si `ng-openapi-gen` ya expone la operación; la fachada `IssuerApiService` mantiene consistencia con `linkStudentTitle`.

### D2: Orden de emisión sin cambio

Anchor → on-chain → link title. Si anchor falla, **no** invocar MetaMask (evita txs huérfanas).

### D3: Manejo de errores en `IssuerTabComponent`

Mapear Problem Details:

| `error` | Mensaje usuario (español) |
|---------|---------------------------|
| `ipfs_not_configured` | IPFS no configurado en el servidor emisor |
| `content_anchor_failed` | No se pudo anclar el documento en IPFS |
| `invalid_anchor_document` | Documento de credencial inválido |

### D4: Sin cálculo de hash en frontend

Eliminar `sha256HexFromString` del flujo de emisión (puede quedar en utils si blockchain lo usa para otra cosa).

### D5: Regeneración de cliente BFF

Ejecutar `ng-openapi-gen` tras snapshot BFF del cambio backend; commitear tipos generados.

## Risks / Trade-offs

| Riesgo | Mitigación |
|--------|------------|
| Despliegue frontend antes que backend | Documentar orden; anchor 404/503 bloquea emisión con mensaje claro |
| Entorno dev sin Pinata en issuer | Fail hard con mensaje; documentar free tier |
| Tests e2e dependían de CID fake | Usar issuer in-memory en integración backend; frontend unit tests mockean `IssuerApiService` |

## Migration Plan

1. Verificar backend `add-issuer-content-anchor` en entorno local.
2. Merge este cambio frontend.
3. Confirmar emisión demo: anchor → MetaMask → listado credenciales.
4. Verificar enlace IPFS en detalle credencial abre gateway con contenido.

## Open Questions

- Ninguna — decisiones cerradas en grilling.
