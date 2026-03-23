FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ExchangeRates.Api.sln ./
COPY kriptoservice/KriptoService/*.csproj ./kriptoservice/KriptoService/
COPY kriptoservice/KriptoService.App/*.csproj ./kriptoservice/KriptoService.App/
COPY kriptoservice/KriptoService.Configuration/*.csproj ./kriptoservice/KriptoService.Configuration/
COPY kriptoservice/KriptoService.DB/*.csproj ./kriptoservice/KriptoService.DB/
COPY kriptoservice/KriptoService.Domain/*.csproj ./kriptoservice/KriptoService.Domain/
COPY kriptoservice/KriptoService.Maintenance/*.csproj ./kriptoservice/KriptoService.Maintenance/
COPY kriptoservice/KriptoService.Migrations/*.csproj ./kriptoservice/KriptoService.Migrations/

RUN dotnet restore kriptoservice/KriptoService/KriptoService.csproj

COPY kriptoservice/KriptoService/ ./kriptoservice/KriptoService/
COPY kriptoservice/KriptoService.App/ ./kriptoservice/KriptoService.App/
COPY kriptoservice/KriptoService.Configuration/ ./kriptoservice/KriptoService.Configuration/
COPY kriptoservice/KriptoService.DB/ ./kriptoservice/KriptoService.DB/
COPY kriptoservice/KriptoService.Domain/ ./kriptoservice/KriptoService.Domain/
COPY kriptoservice/KriptoService.Maintenance/ ./kriptoservice/KriptoService.Maintenance/
COPY kriptoservice/KriptoService.Migrations/ ./kriptoservice/KriptoService.Migrations/

RUN dotnet publish kriptoservice/KriptoService/KriptoService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80 443

RUN mkdir -p /app/data /app/logs

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "KriptoService.dll"]
