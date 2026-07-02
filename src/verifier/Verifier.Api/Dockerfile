FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/verifier/Verifier.Api/Verifier.Api.csproj", "src/verifier/Verifier.Api/"]
COPY ["src/verifier/Verifier.Application/Verifier.Application.csproj", "src/verifier/Verifier.Application/"]
COPY ["src/verifier/Verifier.Domain/Verifier.Domain.csproj", "src/verifier/Verifier.Domain/"]
COPY ["src/verifier/Verifier.Infrastructure/Verifier.Infrastructure.csproj", "src/verifier/Verifier.Infrastructure/"]
RUN dotnet restore "src/verifier/Verifier.Api/Verifier.Api.csproj"

COPY . .
WORKDIR "/src/src/verifier/Verifier.Api"
RUN dotnet build "Verifier.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Verifier.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Verifier.Api.dll"]
