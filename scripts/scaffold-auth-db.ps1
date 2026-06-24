# Regenera el modelo EF Core (database-first) desde Postgres en Docker.
# Requiere: contenedor sovereignid-postgres en red sovereignid_sovereign-net.
# Nota: localhost:5432 puede apuntar a un Postgres local; este script usa la red Docker.

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$infraDir = Join-Path $repoRoot "src/auth/Auth.Infrastructure"
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

$dbContext = Join-Path $infraDir "Persistence/SovereignIdDbContext.cs"
if (Test-Path $dbContext) {
    (Get-Content $dbContext -Raw) `
        -replace 'public partial class SovereignIdDbContext', 'internal partial class SovereignIdDbContext' `
        | Set-Content $dbContext -NoNewline
}

Write-Host "Modelo regenerado en src/auth/Auth.Infrastructure/Persistence"
