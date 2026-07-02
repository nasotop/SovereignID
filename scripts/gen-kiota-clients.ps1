# Regenera clientes Kiota del BFF desde snapshots OpenAPI versionados.
# Requiere: dotnet tool install -g Microsoft.OpenApi.Kiota

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

$clients = @(
    @{ Name = "Verifier"; Contract = "docs/contracts/verifier.openapi.json"; Output = "src/bff/Bff.Clients/Generated/Verifier"; Namespace = "SovereignID.Bff.Clients.Verifier" },
    @{ Name = "Issuer";   Contract = "docs/contracts/issuer.openapi.json";   Output = "src/bff/Bff.Clients/Generated/Issuer";   Namespace = "SovereignID.Bff.Clients.Issuer" },
    @{ Name = "Academy";  Contract = "docs/contracts/academy.openapi.json";  Output = "src/bff/Bff.Clients/Generated/Academy";  Namespace = "SovereignID.Bff.Clients.Academy" },
    @{ Name = "Identity"; Contract = "docs/contracts/identity.openapi.json"; Output = "src/bff/Bff.Clients/Generated/Identity"; Namespace = "SovereignID.Bff.Clients.Identity" }
)

foreach ($client in $clients) {
    Write-Host "Generating Kiota client: $($client.Name)" -ForegroundColor Cyan
    kiota generate `
        -l CSharp `
        -d $client.Contract `
        -o $client.Output `
        -n $client.Namespace `
        --clean-output
}

Write-Host "Kiota clients generated under src/bff/Bff.Clients/Generated/" -ForegroundColor Green
