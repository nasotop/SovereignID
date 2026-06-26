# Regenera el modelo EF Core (database-first) del verifier desde Postgres en Docker.
# Acotado a 4 tablas (ver Verifier.Infrastructure/efcpt-config.json):
#   credentials, verification_logs, credential_types, institutions.
# Requiere: contenedor sovereignid-postgres en red sovereignid_sovereign-net.
# Nota: localhost:5432 puede apuntar a un Postgres local; este script usa la red Docker.
#
# IMPORTANTE — curación post-scaffold: la CLI de EF Core Power Tools (10.x) NO respeta
# el whitelist `tables` del efcpt-config.json y genera las 12 tablas en Persistence/Generated.
# Tras ejecutar este script hay que dejar el modelo acotado a las 4 tablas:
#   1. Eliminar las entidades no incluidas (AuditLog, AuthChallenge, Career,
#      InstitutionMetricsDaily, InstitutionUser, Student, StudentWallet, User) y los
#      auxiliares dbdiagram.md / efcpt-readme.md.
#   2. Mover Credential/CredentialType/Institution/VerificationLog a Persistence/Generated/Entities/
#      y recortar sus navegaciones a las 4 entidades retenidas.
#   3. Recortar VerifierDbContext a los 4 DbSet + sus configuraciones.
# El resultado curado es el que vive versionado en Persistence/Generated/.

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$infraDir = Join-Path $repoRoot "src/verifier/Verifier.Infrastructure"
$network = "sovereignid_sovereign-net"
$conn = "Host=postgres;Port=5432;Database=sovereignid;Username=sovereignid;Password=sovereignid_dev"

if (-not (docker inspect sovereignid-postgres 2>$null)) {
    Write-Error "Contenedor sovereignid-postgres no encontrado. Ejecuta: docker compose up -d postgres"
}

docker run --rm `
    --network $network `
    -v "${infraDir}:/work" `
    -w /work `
    mcr.microsoft.com/dotnet/sdk:10.0 `
    bash -lc "dotnet tool install ErikEJ.EFCorePowerTools.Cli --tool-path /tools --version 10.* >/dev/null && /tools/efcpt '$conn' postgres -v"

$dbContext = Join-Path $infraDir "Persistence/Generated/VerifierDbContext.cs"
if (Test-Path $dbContext) {
    (Get-Content $dbContext -Raw) `
        -replace 'public partial class VerifierDbContext', 'internal partial class VerifierDbContext' `
        | Set-Content $dbContext -NoNewline
}

Write-Host "Modelo regenerado en src/verifier/Verifier.Infrastructure/Persistence/Generated"
