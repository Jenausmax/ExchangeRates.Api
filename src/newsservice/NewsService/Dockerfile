FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ExchangeRates.Api.sln ./
COPY newsservice/NewsService/*.csproj ./newsservice/NewsService/
COPY newsservice/NewsService.App/*.csproj ./newsservice/NewsService.App/
COPY newsservice/NewsService.Configuration/*.csproj ./newsservice/NewsService.Configuration/
COPY newsservice/NewsService.DB/*.csproj ./newsservice/NewsService.DB/
COPY newsservice/NewsService.Domain/*.csproj ./newsservice/NewsService.Domain/
COPY newsservice/NewsService.Maintenance/*.csproj ./newsservice/NewsService.Maintenance/
COPY newsservice/NewsService.Migrations/*.csproj ./newsservice/NewsService.Migrations/

RUN dotnet restore newsservice/NewsService/NewsService.csproj

COPY newsservice/NewsService/ ./newsservice/NewsService/
COPY newsservice/NewsService.App/ ./newsservice/NewsService.App/
COPY newsservice/NewsService.Configuration/ ./newsservice/NewsService.Configuration/
COPY newsservice/NewsService.DB/ ./newsservice/NewsService.DB/
COPY newsservice/NewsService.Domain/ ./newsservice/NewsService.Domain/
COPY newsservice/NewsService.Maintenance/ ./newsservice/NewsService.Maintenance/
COPY newsservice/NewsService.Migrations/ ./newsservice/NewsService.Migrations/

RUN dotnet publish newsservice/NewsService/NewsService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80 443

RUN mkdir -p /app/data /app/logs

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NewsService.dll"]
