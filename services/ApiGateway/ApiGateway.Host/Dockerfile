FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY services/ApiGateway/ApiGateway.Host/*.csproj services/ApiGateway/ApiGateway.Host/
RUN dotnet restore services/ApiGateway/ApiGateway.Host/ApiGateway.Host.csproj

COPY services/ApiGateway/ services/ApiGateway/
WORKDIR /src/services/ApiGateway/ApiGateway.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ApiGateway.Host.dll"]