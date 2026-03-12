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

COPY services/NotificationService/NotificationService.Domain/*.csproj services/NotificationService/NotificationService.Domain/
COPY services/NotificationService/NotificationService.Application/*.csproj services/NotificationService/NotificationService.Application/
COPY services/NotificationService/NotificationService.Infrastructure/*.csproj services/NotificationService/NotificationService.Infrastructure/
COPY services/NotificationService/NotificationService.Host/*.csproj services/NotificationService/NotificationService.Host/

RUN dotnet restore services/NotificationService/NotificationService.Host/NotificationService.Host.csproj

COPY shared/ shared/
COPY services/NotificationService/ services/NotificationService/

WORKDIR /src/services/NotificationService/NotificationService.Host
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "NotificationService.Host.dll"]