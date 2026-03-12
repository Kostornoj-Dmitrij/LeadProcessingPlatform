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
COPY shared/IntegrationEvents/*.csproj shared/IntegrationEvents/

COPY services/LeadService/LeadService.Domain/*.csproj services/LeadService/LeadService.Domain/
COPY services/LeadService/LeadService.Application/*.csproj services/LeadService/LeadService.Application/
COPY services/LeadService/LeadService.Infrastructure/*.csproj services/LeadService/LeadService.Infrastructure/
COPY services/LeadService/LeadService.Host/*.csproj services/LeadService/LeadService.Host/

RUN dotnet restore services/LeadService/LeadService.Host/LeadService.Host.csproj

COPY shared/ shared/
COPY services/LeadService/ services/LeadService/

WORKDIR /src/services/LeadService/LeadService.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "LeadService.Host.dll"]