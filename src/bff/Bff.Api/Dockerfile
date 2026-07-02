FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/bff/Bff.Api/Bff.Api.csproj", "src/bff/Bff.Api/"]
COPY ["src/bff/Bff.Clients/Bff.Clients.csproj", "src/bff/Bff.Clients/"]
RUN dotnet restore "src/bff/Bff.Api/Bff.Api.csproj"

COPY . .
WORKDIR "/src/src/bff/Bff.Api"
RUN dotnet build "Bff.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Bff.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bff.Api.dll"]
