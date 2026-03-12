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

COPY services/ScoringService/ScoringService.Domain/*.csproj services/ScoringService/ScoringService.Domain/
COPY services/ScoringService/ScoringService.Application/*.csproj services/ScoringService/ScoringService.Application/
COPY services/ScoringService/ScoringService.Infrastructure/*.csproj services/ScoringService/ScoringService.Infrastructure/
COPY services/ScoringService/ScoringService.Host/*.csproj services/ScoringService/ScoringService.Host/

RUN dotnet restore services/ScoringService/ScoringService.Host/ScoringService.Host.csproj

COPY shared/ shared/
COPY services/ScoringService/ services/ScoringService/

WORKDIR /src/services/ScoringService/ScoringService.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "ScoringService.Host.dll"]