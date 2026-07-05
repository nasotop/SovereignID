FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/reports/Reports.Api/Reports.Api.csproj", "src/reports/Reports.Api/"]
COPY ["src/reports/Reports.Application/Reports.Application.csproj", "src/reports/Reports.Application/"]
COPY ["src/reports/Reports.Domain/Reports.Domain.csproj", "src/reports/Reports.Domain/"]
COPY ["src/reports/Reports.Infrastructure/Reports.Infrastructure.csproj", "src/reports/Reports.Infrastructure/"]
COPY ["src/common/SovereignID.Authorization/SovereignID.Authorization.csproj", "src/common/SovereignID.Authorization/"]
RUN dotnet restore "src/reports/Reports.Api/Reports.Api.csproj"

COPY . .
WORKDIR "/src/src/reports/Reports.Api"
RUN dotnet build "Reports.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Reports.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --quiet --tries=1 -O /dev/null http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Reports.Api.dll"]
