FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/identity/Identity.Api/Identity.Api.csproj", "src/identity/Identity.Api/"]
COPY ["src/identity/Identity.Application/Identity.Application.csproj", "src/identity/Identity.Application/"]
COPY ["src/identity/Identity.Domain/Identity.Domain.csproj", "src/identity/Identity.Domain/"]
COPY ["src/identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/identity/Identity.Infrastructure/"]
RUN dotnet restore "src/identity/Identity.Api/Identity.Api.csproj"

COPY . .
WORKDIR "/src/src/identity/Identity.Api"
RUN dotnet build "Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
