FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY services/ApiGateway/ApiGateway.Host/*.csproj services/ApiGateway/ApiGateway.Host/
RUN dotnet restore services/ApiGateway/ApiGateway.Host/ApiGateway.Host.csproj

COPY . .

WORKDIR /src/services/ApiGateway/ApiGateway.Host
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY _infrastructure/scripts/wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ApiGateway.Host.dll"]