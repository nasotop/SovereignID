# Regenera el modelo EF Core (database-first) del servicio academy desde Postgres local.
# Requiere: contenedor sovereignid-postgres publicado en localhost:5432.

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$infraDir = Join-Path $repoRoot "src/academy/Academy.Infrastructure"
$conn = "Host=127.0.0.1;Port=5432;Database=sovereignid;Username=sovereignid;Password=sovereignid_dev"

Push-Location $infraDir
try {
    efcpt $conn postgres -i efcpt-config.json -v
}
finally {
    Pop-Location
}

$generatedDir = Join-Path $infraDir "Persistence/Generated"
Get-ChildItem -Path $generatedDir -Filter "*.cs" | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $content = $content -replace "public partial class", "internal partial class"
    [System.IO.File]::WriteAllText($_.FullName, $content)
}

Write-Host "Modelo regenerado en src/academy/Academy.Infrastructure/Persistence/Generated"
