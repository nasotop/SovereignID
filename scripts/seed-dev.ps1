# Aplica database/seed-dev.sql al Postgres de desarrollo en Docker.
# Requiere: contenedor sovereignid-postgres healthy.
#
# Uso:
#   docker compose up -d postgres
#   .\scripts\seed-dev.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$seedFile = Join-Path $repoRoot "database/seed-dev.sql"
$container = "sovereignid-postgres"
$dbName = "sovereignid"
$dbUser = "sovereignid"

if (-not (Test-Path $seedFile)) {
    Write-Error "No se encontro $seedFile"
}

$containerInfo = docker inspect $container 2>$null
if (-not $containerInfo) {
    Write-Error "Contenedor $container no encontrado. Ejecuta: docker compose up -d postgres"
}

docker exec $container pg_isready -U $dbUser -d $dbName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Postgres no esta listo en $container (pg_isready fallo)."
}

$actualDb = docker exec $container psql -U $dbUser -d $dbName -tAc "SELECT current_database();"
if ($actualDb.Trim() -ne $dbName) {
    Write-Error "Base de datos inesperada: '$($actualDb.Trim())' (esperado: $dbName)."
}

Write-Host "Aplicando seed de desarrollo en $container / $dbName ..."
docker cp $seedFile "${container}:/tmp/seed-dev.sql"
if ($LASTEXITCODE -ne 0) {
    Write-Error "No se pudo copiar seed-dev.sql al contenedor."
}

docker exec $container psql -U $dbUser -d $dbName -v ON_ERROR_STOP=1 -f /tmp/seed-dev.sql
if ($LASTEXITCODE -ne 0) {
    Write-Error "seed-dev.sql fallo. Revisa el output de psql arriba."
}

docker exec $container rm /tmp/seed-dev.sql | Out-Null

Write-Host "Seed aplicado."
Write-Host ""
Write-Host "Titular demo:"
Write-Host "  wallet: 0xf6461f392288b5732a7703e8b83f64cab134eada"
Write-Host "  did:    did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada"
Write-Host ""
Write-Host "Credenciales (verifier / holder):"
Write-Host "  aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa  TITULO         active"
Write-Host "  bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb  NOTAS          revoked"
Write-Host "  cccccccc-cccc-cccc-cccc-cccccccccccc  CERTIFICACION  expired"
Write-Host ""
Write-Host "Siguiente paso: docker compose up -d issuer-api verifier-api auth-api web"
