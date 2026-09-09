FROM mcr.microsoft.com/dotnet/sdk:10.0.401-alpine3.23 AS build

WORKDIR /src
COPY .editorconfig global.json Directory.Build.props WindowsAssetInventory.sln ./
COPY src/WindowsAssetInventory/WindowsAssetInventory.csproj src/WindowsAssetInventory/
COPY src/WindowsAssetInventory/packages.lock.json src/WindowsAssetInventory/
COPY tests/WindowsAssetInventory.Tests/WindowsAssetInventory.Tests.csproj tests/WindowsAssetInventory.Tests/
COPY tests/WindowsAssetInventory.Tests/packages.lock.json tests/WindowsAssetInventory.Tests/
RUN dotnet restore WindowsAssetInventory.sln --locked-mode

COPY src/WindowsAssetInventory/ src/WindowsAssetInventory/
RUN dotnet publish src/WindowsAssetInventory/WindowsAssetInventory.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10-alpine3.23 AS runtime

WORKDIR /app
COPY --from=build --chown=app:app /app/publish ./
RUN mkdir -p /app/data && chown app:app /app/data

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Inventory="Data Source=/app/data/inventory.db"

USER app
EXPOSE 8080
VOLUME ["/app/data"]

HEALTHCHECK --interval=15s --timeout=3s --start-period=15s --retries=3 \
    CMD wget -q --spider http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "WindowsAssetInventory.dll"]
