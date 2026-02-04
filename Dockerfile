# Этап 1: Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

# Копируем .sln и .csproj файлы для restore
COPY src/*.sln ./
COPY src/ExchangeRates.Api/*.csproj ./ExchangeRates.Api/
COPY src/ExchangeRates.Configuration/*.csproj ./ExchangeRates.Configuration/
COPY src/ExchangeRates.Core.App/*.csproj ./ExchangeRates.Core.App/
COPY src/ExchangeRates.Core.Domain/*.csproj ./ExchangeRates.Core.Domain/
COPY src/ExchangeRates.Infrastructure.DB/*.csproj ./ExchangeRates.Infrastructure.DB/
COPY src/ExchangeRates.Infrastructure.SQLite/*.csproj ./ExchangeRates.Infrastructure.SQLite/
COPY src/ExchangeRates.Maintenance/*.csproj ./ExchangeRates.Maintenance/
COPY src/ExchangeRates.Migrations/*.csproj ./ExchangeRates.Migrations/

# Restore зависимостей
RUN dotnet restore ExchangeRates.Api.sln

# Копируем остальные файлы и собираем
COPY src/ ./
RUN dotnet publish ExchangeRates.Api/ExchangeRates.Api.csproj -c Release -o /app/publish

# Этап 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app

# Копируем собранное приложение
COPY --from=build /app/publish .

# Открываем порт
EXPOSE 80
EXPOSE 443

# Создаем директории для БД и логов
RUN mkdir -p /app/data /app/logs

# Переменные окружения
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Запуск приложения
ENTRYPOINT ["dotnet", "ExchangeRates.Api.dll"]
