FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Aseguramos ejecución segura sin root
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia de proyectos para restaurar dependencias
COPY ["src/auth/Auth.Api/Auth.Api.csproj", "src/auth/Auth.Api/"]
COPY ["src/auth/Auth.Application/Auth.Application.csproj", "src/auth/Auth.Application/"]
COPY ["src/auth/Auth.Domain/Auth.Domain.csproj", "src/auth/Auth.Domain/"]
COPY ["src/auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/auth/Auth.Infrastructure/"]
RUN dotnet restore "src/auth/Auth.Api/Auth.Api.csproj"

# Copia del código fuente completo
COPY . .
WORKDIR "/src/src/auth/Auth.Api"
RUN dotnet build "Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]