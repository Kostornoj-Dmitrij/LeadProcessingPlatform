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

COPY services/DistributionService/DistributionService.Domain/*.csproj services/DistributionService/DistributionService.Domain/
COPY services/DistributionService/DistributionService.Application/*.csproj services/DistributionService/DistributionService.Application/
COPY services/DistributionService/DistributionService.Infrastructure/*.csproj services/DistributionService/DistributionService.Infrastructure/
COPY services/DistributionService/DistributionService.Host/*.csproj services/DistributionService/DistributionService.Host/

RUN dotnet restore services/DistributionService/DistributionService.Host/DistributionService.Host.csproj

COPY shared/ shared/
COPY services/DistributionService/ services/DistributionService/

WORKDIR /src/services/DistributionService/DistributionService.Host
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

ENTRYPOINT ["dotnet", "DistributionService.Host.dll"]