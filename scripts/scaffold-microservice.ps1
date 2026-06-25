param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [int]$HttpPort,

    [Parameter(Mandatory = $true)]
    [int]$HttpsPort,

    [Parameter(Mandatory = $true)]
    [string]$UserSecretsId
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$folder = $Name.ToLowerInvariant()
$base = Join-Path $root "src\$folder"

$projects = @(
    "$Name.Domain",
    "$Name.Application",
    "$Name.Infrastructure",
    "$Name.Api"
)

foreach ($project in $projects) {
    New-Item -ItemType Directory -Force -Path (Join-Path $base $project) | Out-Null
}

# Domain
@"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Domain\$Name.Domain.csproj")

# Application
@"
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\$Name.Domain\$Name.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.8" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Application\$Name.Application.csproj")

@"
namespace $Name.Application;

public sealed class ${Name}Options
{
    public const string SectionName = "$Name";
}
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Application\${Name}Options.cs")

# Infrastructure
@"
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\$Name.Application\$Name.Application.csproj" />
    <ProjectReference Include="..\$Name.Domain\$Name.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.8" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.8" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Infrastructure\$Name.Infrastructure.csproj")

$persistenceDir = Join-Path $base "$Name.Infrastructure\Persistence\Composition"
New-Item -ItemType Directory -Force -Path $persistenceDir | Out-Null

@"
namespace ${Name}.Infrastructure.Persistence.Composition;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string Provider { get; set; } = PersistenceProviders.InMemory;
}

public static class PersistenceProviders
{
    public const string InMemory = "InMemory";
    public const string Postgres = "Postgres";
}
"@ | Set-Content -Encoding utf8 (Join-Path $persistenceDir "PersistenceOptions.cs")

@"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ${Name}.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection Add${Name}PostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }

        return services;
    }

    public static bool UsesPostgresPersistence(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider
            ?? PersistenceProviders.InMemory,
            PersistenceProviders.Postgres,
            StringComparison.OrdinalIgnoreCase);
}
"@ | Set-Content -Encoding utf8 (Join-Path $persistenceDir "PersistenceServiceCollectionExtensions.cs")

@"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ${Name}.Infrastructure.Persistence.Composition;

public static class ${Name}PersistenceServiceCollectionExtensions
{
    public static IServiceCollection Add${Name}Persistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration))
        {
            services.Add${Name}PostgresPersistence(configuration);
        }

        return services;
    }
}
"@ | Set-Content -Encoding utf8 (Join-Path $persistenceDir "${Name}PersistenceServiceCollectionExtensions.cs")

@"
using ${Name}.Application;
using ${Name}.Infrastructure.Persistence.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ${Name}.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection Add${Name}Infrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<${Name}Options>(configuration.GetSection(${Name}Options.SectionName));
        services.Add${Name}Persistence(configuration);

        return services;
    }

    public static void Validate${Name}Configuration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration)
            && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }
    }
}
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Infrastructure\DependencyInjection.cs")

# Api
@"
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>$UserSecretsId</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..\..</DockerfileContext>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.8" />
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.23.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\$Name.Application\$Name.Application.csproj" />
    <ProjectReference Include="..\$Name.Infrastructure\$Name.Infrastructure.csproj" />
  </ItemGroup>

</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\$Name.Api.csproj")

@"
using ${Name}.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.Add${Name}Infrastructure(builder.Configuration);

var app = builder.Build();

app.Validate${Name}Configuration();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\Program.cs")

@"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Persistence": {
    "Provider": "InMemory"
  },
  "$Name": {}
}
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\appsettings.json")

@"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Persistence": {
    "Provider": "InMemory"
  }
}
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\appsettings.Development.json")

$propsDir = Join-Path $base "$Name.Api\Properties"
New-Item -ItemType Directory -Force -Path $propsDir | Out-Null

@"
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:$HttpPort"
    },
    "https": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "dotnetRunMessages": true,
      "applicationUrl": "https://localhost:$HttpsPort;http://localhost:$HttpPort"
    },
    "Container (Dockerfile)": {
      "commandName": "Docker",
      "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}",
      "environmentVariables": {
        "ASPNETCORE_HTTPS_PORTS": "8081",
        "ASPNETCORE_HTTP_PORTS": "8080"
      },
      "publishAllPorts": true,
      "useSSL": true
    }
  },
  "`$schema": "https://json.schemastore.org/launchsettings.json"
}
"@ | Set-Content -Encoding utf8 (Join-Path $propsDir "launchSettings.json")

$controllersDir = Join-Path $base "$Name.Api\Controllers"
New-Item -ItemType Directory -Force -Path $controllersDir | Out-Null

@"
using Microsoft.AspNetCore.Mvc;

namespace ${Name}.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "$($Name.ToLowerInvariant())" });
}
"@ | Set-Content -Encoding utf8 (Join-Path $controllersDir "HealthController.cs")

@"
@$Name.Api_HostAddress = http://localhost:$HttpPort

GET {{$Name.Api_HostAddress}}/health
Accept: application/json

###
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\$Name.Api.http")

@"
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/$folder/$Name.Api/$Name.Api.csproj", "src/$folder/$Name.Api/"]
COPY ["src/$folder/$Name.Application/$Name.Application.csproj", "src/$folder/$Name.Application/"]
COPY ["src/$folder/$Name.Domain/$Name.Domain.csproj", "src/$folder/$Name.Domain/"]
COPY ["src/$folder/$Name.Infrastructure/$Name.Infrastructure.csproj", "src/$folder/$Name.Infrastructure/"]
RUN dotnet restore "src/$folder/$Name.Api/$Name.Api.csproj"

COPY . .
WORKDIR "/src/src/$folder/$Name.Api"
RUN dotnet build "$Name.Api.csproj" -c `$BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "$Name.Api.csproj" -c `$BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "$Name.Api.dll"]
"@ | Set-Content -Encoding utf8 (Join-Path $base "$Name.Api\Dockerfile")

Copy-Item -Force (Join-Path $root "src\auth\Auth.Api\.dockerignore") (Join-Path $base "$Name.Api\.dockerignore")

# Integration tests
$testBase = Join-Path $root "tests\$folder\$Name.IntegrationTests"
New-Item -ItemType Directory -Force -Path $testBase | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.8" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\$folder\$Name.Api\$Name.Api.csproj" />
  </ItemGroup>

</Project>
"@ | Set-Content -Encoding utf8 (Join-Path $testBase "$Name.IntegrationTests.csproj")

@"
using ${Name}.Infrastructure.Persistence.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ${Name}.IntegrationTests;

public sealed class ${Name}WebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [`$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory
            });
        });
    }
}
"@ | Set-Content -Encoding utf8 (Join-Path $testBase "${Name}WebApplicationFactory.cs")

@"
using System.Net;

namespace ${Name}.IntegrationTests;

public sealed class ${Name}ApiTests : IClassFixture<${Name}WebApplicationFactory>
{
    private readonly HttpClient _client;

    public ${Name}ApiTests(${Name}WebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_IsAvailable_InDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
"@ | Set-Content -Encoding utf8 (Join-Path $testBase "${Name}ApiTests.cs")

Write-Host "Scaffolded $Name at src/$folder"
