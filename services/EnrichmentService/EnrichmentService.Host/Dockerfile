FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LeadProcessingPlatform.sln .
COPY shared/SharedKernel/*.csproj shared/SharedKernel/
COPY shared/AvroSchemas/*.csproj shared/AvroSchemas/

COPY services/EnrichmentService/EnrichmentService.Domain/*.csproj services/EnrichmentService/EnrichmentService.Domain/
COPY services/EnrichmentService/EnrichmentService.Application/*.csproj services/EnrichmentService/EnrichmentService.Application/
COPY services/EnrichmentService/EnrichmentService.Infrastructure/*.csproj services/EnrichmentService/EnrichmentService.Infrastructure/
COPY services/EnrichmentService/EnrichmentService.Host/*.csproj services/EnrichmentService/EnrichmentService.Host/

RUN dotnet restore services/EnrichmentService/EnrichmentService.Host/EnrichmentService.Host.csproj

COPY shared/ shared/
COPY services/EnrichmentService/ services/EnrichmentService/

WORKDIR /src/services/EnrichmentService/EnrichmentService.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY _infrastructure/scripts/wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "EnrichmentService.Host.dll"]