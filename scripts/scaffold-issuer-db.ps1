# Regenera el modelo EF Core (database-first) del servicio issuer desde Postgres en Docker.
# Requiere: contenedor sovereignid-postgres en red sovereignid_sovereign-net.

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$infraDir = Join-Path $repoRoot "src/issuer/Issuer.Infrastructure"
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
    bash -lc "dotnet tool install ErikEJ.EFCorePowerTools.Cli --tool-path /tools --version 10.* >/dev/null && /tools/efcpt '$conn' postgres -i efcpt-config.json -v"

$dbContext = Join-Path $infraDir "Persistence/Generated/IssuerDbContext.cs"
if (Test-Path $dbContext) {
    $content = Get-Content $dbContext -Raw
    if ($content -match 'public partial class IssuerDbContext') {
        $content = $content -replace 'public partial class IssuerDbContext', 'internal partial class IssuerDbContext'
        [System.IO.File]::WriteAllText($dbContext, $content)
    }
}

Write-Host "Modelo regenerado en src/issuer/Issuer.Infrastructure/Persistence/Generated"
