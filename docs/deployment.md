# Despliegue — CI/CD y Portainer

SovereignID se despliega en una VPS gestionada con **Portainer**. El pipeline de GitHub Actions valida el código en cada PR/push y despliega automáticamente a producción cuando se hace merge o push a **`master`**.

## Flujo de ramas

```
feature/*  →  PR  →  dev  →  PR  →  master  →  deploy automático (VPS)
```

| Rama | Propósito |
|------|-----------|
| `feature/*` | Desarrollo de funcionalidades |
| `dev` | Integración; todo merge debe pasar CI y SonarCloud antes de fusionar |
| `master` | Producción; cada push dispara redeploy en Portainer |

## Pipeline de GitHub Actions

Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

| Job | Cuándo | Qué hace |
|-----|--------|----------|
| `build-frontend` | push/PR a `dev` o `master` | Build Angular producción |
| `test-frontend` | push/PR a `dev` o `master` | Tests unitarios y de contrato |
| `build-backend` | push/PR a `dev` o `master` | Build .NET Release |
| `test-backend` | push/PR a `dev` o `master` | Tests de integración auth |
| `verify-openapi` | push/PR a `dev` o `master` | Snapshots OpenAPI al día |
| `deploy-production` | **solo push a `master`** | POST al webhook de Portainer |

El job `deploy-production` solo corre si todos los jobs anteriores pasan.

### SonarCloud

El análisis de calidad lo gestiona **SonarCloud vía GitHub App** (no hay step de scanner en el workflow). Configurar branch protection en GitHub para exigir el check de SonarCloud en PRs hacia `dev` y `master`.

## Configuración en GitHub

### Secrets (Settings → Secrets and variables → Actions)

| Secret | Obligatorio | Descripción |
|--------|-------------|-------------|
| `PORTAINER_WEBHOOK_URL` | Sí (para CD) | URL del webhook del stack en Portainer |

### Environment `production` (opcional)

El job de deploy usa `environment: production`. En **Settings → Environments → production** puedes activar:

- Required reviewers (aprobación manual antes del deploy)
- Deployment branches: solo `master`

### Branch protection

**Rama `dev`:**

- Require a pull request before merging
- Require status checks to pass:
  - Jobs del workflow `SovereignID CI Pipeline`
  - SonarCloud / Quality Gate

**Rama `master`:**

- Require a pull request before merging (idealmente desde `dev`)
- Require status checks to pass:
  - Jobs del workflow `SovereignID CI Pipeline`
  - SonarCloud / Quality Gate

## Configuración en Portainer

1. **Stacks** → stack SovereignID → verificar origen Git:
   - Repository: `https://github.com/nasotop/SovereignID`
   - Branch: **`master`**
   - Compose path: **`docker-compose.yml`**
2. Activar **Webhook** (Pull and redeploy via webhook).
3. Copiar la URL generada (formato típico: `https://<host>/api/stacks/webhooks/<id>`).
4. Pegar esa URL en GitHub como secret `PORTAINER_WEBHOOK_URL`.
5. Variables de entorno del stack ya definidas en Portainer (`AUTH_JWT_SIGNING_KEY`, `POSTGRES_*`, `WEB_HOST_PORT`, etc.) — ver [`.env.example`](../.env.example).

Al recibir el webhook, Portainer clona el repo, reconstruye las imágenes (`build:` en compose) y levanta el stack.

## Verificación tras deploy

1. **GitHub Actions:** job "Deploy to Portainer" en verde.
2. **Portainer:** stack muestra redeploy reciente sin errores.
3. **VPS:** `docker ps --filter name=sovereignid` — contenedores `healthy`.
4. **Aplicación:** frontend y `GET /auth/nonce` responden correctamente.

## Si el deploy falla

| Síntoma | Acción |
|---------|--------|
| Job deploy falla con curl error | Verificar `PORTAINER_WEBHOOK_URL` en GitHub Secrets |
| Portainer redeploy falla | Revisar logs del stack en Portainer (build, variables faltantes) |
| `auth-api` unhealthy | Revisar `AUTH_JWT_SIGNING_KEY` y logs del contenedor |
| CI no se ejecuta | Confirmar que el push/PR es a `dev` o `master` (no `main`/`develop`) |

Redeploy manual de respaldo: Portainer → stack → **Pull and redeploy**.
