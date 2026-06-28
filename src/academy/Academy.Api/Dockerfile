FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/academy/Academy.Api/Academy.Api.csproj", "src/academy/Academy.Api/"]
COPY ["src/academy/Academy.Application/Academy.Application.csproj", "src/academy/Academy.Application/"]
COPY ["src/academy/Academy.Domain/Academy.Domain.csproj", "src/academy/Academy.Domain/"]
COPY ["src/academy/Academy.Infrastructure/Academy.Infrastructure.csproj", "src/academy/Academy.Infrastructure/"]
RUN dotnet restore "src/academy/Academy.Api/Academy.Api.csproj"

COPY . .
WORKDIR "/src/src/academy/Academy.Api"
RUN dotnet build "Academy.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Academy.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Academy.Api.dll"]
