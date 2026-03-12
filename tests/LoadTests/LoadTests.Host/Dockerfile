FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY tests/LoadTests/LoadTests.Host/*.csproj tests/LoadTests/LoadTests.Host/
RUN dotnet restore tests/LoadTests/LoadTests.Host/LoadTests.Host.csproj

COPY tests/ tests/
WORKDIR /src/tests/LoadTests/LoadTests.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LoadTests.Host.dll"]