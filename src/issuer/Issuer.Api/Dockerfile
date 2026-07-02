FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/issuer/Issuer.Api/Issuer.Api.csproj", "src/issuer/Issuer.Api/"]
COPY ["src/issuer/Issuer.Application/Issuer.Application.csproj", "src/issuer/Issuer.Application/"]
COPY ["src/issuer/Issuer.Domain/Issuer.Domain.csproj", "src/issuer/Issuer.Domain/"]
COPY ["src/issuer/Issuer.Infrastructure/Issuer.Infrastructure.csproj", "src/issuer/Issuer.Infrastructure/"]
RUN dotnet restore "src/issuer/Issuer.Api/Issuer.Api.csproj"

COPY . .
WORKDIR "/src/src/issuer/Issuer.Api"
RUN dotnet build "Issuer.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Issuer.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Issuer.Api.dll"]
