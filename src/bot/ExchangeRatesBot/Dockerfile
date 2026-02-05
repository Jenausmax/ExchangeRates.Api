# Этап 1: Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

# Копируем .sln и .csproj файлы для restore
COPY ExchangeRates.Api.sln ./
COPY bot/ExchangeRatesBot/*.csproj ./bot/ExchangeRatesBot/
COPY bot/ExchangeRatesBot.App/*.csproj ./bot/ExchangeRatesBot.App/
COPY bot/ExchangeRatesBot.Configuration/*.csproj ./bot/ExchangeRatesBot.Configuration/
COPY bot/ExchangeRatesBot.DB/*.csproj ./bot/ExchangeRatesBot.DB/
COPY bot/ExchangeRatesBot.Domain/*.csproj ./bot/ExchangeRatesBot.Domain/
COPY bot/ExchangeRatesBot.Maintenance/*.csproj ./bot/ExchangeRatesBot.Maintenance/
COPY bot/ExchangeRatesBot.Migrations/*.csproj ./bot/ExchangeRatesBot.Migrations/

# Restore зависимостей только для проектов бота
RUN dotnet restore bot/ExchangeRatesBot/ExchangeRatesBot.csproj

# Копируем остальные файлы проектов бота и собираем
COPY bot/ExchangeRatesBot/ ./bot/ExchangeRatesBot/
COPY bot/ExchangeRatesBot.App/ ./bot/ExchangeRatesBot.App/
COPY bot/ExchangeRatesBot.Configuration/ ./bot/ExchangeRatesBot.Configuration/
COPY bot/ExchangeRatesBot.DB/ ./bot/ExchangeRatesBot.DB/
COPY bot/ExchangeRatesBot.Domain/ ./bot/ExchangeRatesBot.Domain/
COPY bot/ExchangeRatesBot.Maintenance/ ./bot/ExchangeRatesBot.Maintenance/
COPY bot/ExchangeRatesBot.Migrations/ ./bot/ExchangeRatesBot.Migrations/

RUN dotnet publish bot/ExchangeRatesBot/ExchangeRatesBot.csproj -c Release -o /app/publish

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
ENTRYPOINT ["dotnet", "ExchangeRatesBot.dll"]
