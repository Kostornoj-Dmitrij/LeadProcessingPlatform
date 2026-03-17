FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LeadProcessingPlatform.sln ./
COPY services/LeadService/LeadService.Host/*.csproj services/LeadService/LeadService.Host/
COPY services/LeadService/LeadService.Application/*.csproj services/LeadService/LeadService.Application/
COPY services/LeadService/LeadService.Domain/*.csproj services/LeadService/LeadService.Domain/
COPY services/LeadService/LeadService.Infrastructure/*.csproj services/LeadService/LeadService.Infrastructure/
COPY shared/SharedKernel/*.csproj shared/SharedKernel/
COPY shared/IntegrationEvents/*.csproj shared/IntegrationEvents/

RUN dotnet restore services/LeadService/LeadService.Host/LeadService.Host.csproj

COPY . .

WORKDIR /src/services/LeadService/LeadService.Host
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY _infrastructure/scripts/wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LeadService.Host.dll"]