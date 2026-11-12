FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/LocalizadorCafes.sln backend/
COPY backend/src/LocalizadorCafes.Domain/LocalizadorCafes.Domain.csproj backend/src/LocalizadorCafes.Domain/
COPY backend/src/LocalizadorCafes.Application/LocalizadorCafes.Application.csproj backend/src/LocalizadorCafes.Application/
COPY backend/src/LocalizadorCafes.Infrastructure/LocalizadorCafes.Infrastructure.csproj backend/src/LocalizadorCafes.Infrastructure/
COPY backend/src/LocalizadorCafes.Api/LocalizadorCafes.Api.csproj backend/src/LocalizadorCafes.Api/
COPY backend/tests/LocalizadorCafes.UnitTests/LocalizadorCafes.UnitTests.csproj backend/tests/LocalizadorCafes.UnitTests/
RUN dotnet restore backend/LocalizadorCafes.sln

COPY backend/ backend/
RUN dotnet publish backend/src/LocalizadorCafes.Api/LocalizadorCafes.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "LocalizadorCafes.Api.dll"]
