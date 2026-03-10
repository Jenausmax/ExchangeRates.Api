# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Контекст разработчика

Я - Мартин, сеньор .NET разработчик с 20-летним стажем. Работаю на проекте под руководством Макса. Вся коммуникация ведется на русском языке.

## Обзор проекта

Монорепозиторий содержит две связанные системы:

1. **ExchangeRates.Api** - ASP.NET Core 5.0 API для получения и хранения курсов валют от ЦБ РФ
2. **ExchangeRatesBot** - Telegram бот для предоставления курсов валют пользователям

Обе системы используют SQLite для хранения данных (каждая свою БД) и развертываются через docker-compose как взаимосвязанные микросервисы.

## Команды для сборки и запуска

### Локальная разработка

```bash
# Сборка решения (из корня репозитория)
dotnet build src/ExchangeRates.Api.sln

# Запуск API
dotnet run --project src/ExchangeRates.Api/ExchangeRates.Api.csproj

# Запуск Telegram бота
dotnet run --project src/bot/ExchangeRatesBot/ExchangeRatesBot.csproj

# Entity Framework миграции для API
dotnet ef database update --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api
dotnet ef migrations add <MigrationName> --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api

# Entity Framework миграции для бота
dotnet ef database update --project src/bot/ExchangeRatesBot.Migrations --startup-project src/bot/ExchangeRatesBot
dotnet ef migrations add <MigrationName> --project src/bot/ExchangeRatesBot.Migrations --startup-project src/bot/ExchangeRatesBot
```

### Docker (рекомендуется)

```bash
# Запуск обоих сервисов (из корня проекта)
docker-compose up -d

# Остановка
docker-compose down

# Просмотр логов
docker-compose logs -f
docker-compose logs -f exchangerates-api
docker-compose logs -f exchangerates-bot

# Пересборка и перезапуск
docker-compose up -d --build

# Запуск только API
docker-compose up -d exchangerates-api

# Запуск только бота (требует запущенный API)
docker-compose up -d exchangerates-bot
```

**ВАЖНО**: Создайте файл `.env` в корне проекта с секретами:
```bash
BOT_TOKEN=your_telegram_bot_token
BOT_USE_POLLING=true
BOT_WEBHOOK=
BOT_TIME_ONE=14:05
BOT_TIME_TWO=15:32
```

## Процесс планирования фич

При планировании новых фич (features) следуйте этому процессу:

1. **Создание плана**: При входе в режим планирования (EnterPlanMode) создавайте файл плана в папке `doc/feature/` с именем `ГГГГММДД-название-фичи.md` (например, `20260206-интеграция-telegram-бота-в-docker-compose.md`)

2. **Формат файла плана**:
   - В самом начале файла (первая строка) должен быть статус реализации в формате markdown checkbox:
     - `- [ ] Не реализовано` - фича еще не реализована
     - `- [x] Реализовано` - фича полностью реализована
   - После статуса следует полное описание плана реализации

3. **Обновление статуса**: После завершения реализации фичи обновите статус в файле плана с `- [ ] Не реализовано` на `- [x] Реализовано`

4. **Структура папки doc**: Вся документация проекта хранится в папке `doc/` в корне репозитория. Планы фич находятся в `doc/feature/` для удобного отслеживания и истории разработки

## Архитектура решения

Решение использует чистую/слоистую архитектуру с разделением на два независимых приложения.

### ExchangeRates.Api (Сервис курсов валют)

#### Слой ядра (Core Layer)
- **ExchangeRates.Core.Domain**: Доменные модели (`Root`, `Valute`, `ParseModel`) и интерфейсы (`IApiClient`, `IProcessingService`, `ISaveService`, `IGetValute`, `IRepositoryBase<T>`)
- **ExchangeRates.Core.App**: Сервисы приложения
  - `ApiClientService`: HTTP-клиент для API ЦБ РФ (https://www.cbr-xml-daily.ru/daily_json.js)
  - `ProcessingService`: Получение и десериализация JSON
  - `SaveService`: Маппинг в модели БД и сохранение (ручной маппинг 34 валют для типобезопасности)
  - `GetValuteService`: Получение исторических курсов из БД

#### Инфраструктурный слой
- **ExchangeRates.Infrastructure.DB**: EF Core контекст `DataDb` и модели БД (`ValuteModelDb`)
- **ExchangeRates.Infrastructure.SQLite**: Generic-репозиторий `RepositoryDbSQLite<T>`
- **ExchangeRates.Migrations**: EF Core миграции

#### Слой представления
- **ExchangeRates.Api**: Web API с `ValuteController` (POST эндпоинт для запроса курсов по коду валюты и диапазону дней)

#### Фоновые задачи
- **ExchangeRates.Maintenance**: Инфраструктура фоновых задач
  - `BackgroundTaskAbstract<T>`: Базовый класс для периодических задач
  - `JobsCreateValute`: Запланированная задача (раз в день в `TimeUpdateJobs`)
  - `JobsCreateValuteToHour`: Периодическая задача (каждые `PeriodMinute` минут) с предотвращением дубликатов

**Поток данных**:
1. Фоновая задача срабатывает → 2. `ProcessingService.RequestProcessing()` → 3. Десериализация JSON → 4. `SaveService.SaveSet()` или `SaveSetNoDublicate()` → 5. `RepositoryDbSQLite<T>` сохраняет в SQLite → 6. API эндпоинт возвращает данные

### ExchangeRatesBot (Telegram бот)

#### Архитектура слоев
- **ExchangeRatesBot.Domain**: Интерфейсы и доменные модели
- **ExchangeRatesBot.App**: Сервисы приложения
  - `BotService`: Singleton, управляет `TelegramBotClient` и режимами работы (webhook/polling)
  - `UpdateService`: Обработка входящих Update от Telegram
  - `CommandService`: Парсинг и выполнение команд (/start, /stop, /help, /курс)
  - `MessageValuteService`: Получение данных курсов от ExchangeRates.Api
  - `ApiClientService`: HTTP-клиент для обращения к ExchangeRates.Api
  - `UserService`: Работа с пользователями (подписка/отписка)
- **ExchangeRatesBot.DB**: EF Core контекст `DataDb` и модели БД
- **ExchangeRatesBot.Migrations**: EF Core миграции
- **ExchangeRatesBot.Maintenance**: Фоновые задачи
  - `JobsSendMessageUsers`: Рассылка курсов подписанным пользователям (`TimeOne`, `TimeTwo`)
  - `PollingBackgroundService`: Long polling для получения обновлений от Telegram (альтернатива webhook)
- **ExchangeRatesBot**: ASP.NET Core хост с `UpdateController` для webhook

#### Режимы работы бота

Бот поддерживает два режима (управляется через `BotConfig.UsePolling`):

**1. Webhook режим (UsePolling=false)**:
- Telegram отправляет обновления на публичный HTTPS URL
- Требуется настроенный `BotConfig.Webhook`
- `BotService` вызывает `SetWebhookAsync()` при инициализации
- `UpdateController` принимает POST запросы от Telegram

**2. Polling режим (UsePolling=true, рекомендуется для Docker)**:
- Бот сам опрашивает Telegram API через `GetUpdatesAsync`
- Не требуется публичный домен
- `BotService` вызывает `DeleteWebhookAsync()` при инициализации
- `PollingBackgroundService` работает в бесконечном цикле с long polling (timeout=30 сек)
- Offset management: `offset = update.Id + 1` после каждого обновления

**Важно**: `CommandService` универсален и переиспользуется для обоих режимов без изменений.

#### Поток обработки команд:
1. Update приходит (webhook POST или polling GetUpdatesAsync) → 2. `ICommandBot.SetCommandBot()` → 3. Парсинг команды → 4. Вызов соответствующего обработчика → 5. Отправка ответа через `BotService.Client`

### Docker Compose архитектура

Два сервиса в общей сети `exchangerates-network`:

- **exchangerates-api**: Порты 5000:80, 5001:443
  - Volumes: `./data` (Data.db), `./logs`
  - Auto-migration при старте (`dataDb.Database.Migrate()`)

- **exchangerates-bot**: Зависит от exchangerates-api
  - Обращается к API по имени сервиса: `http://exchangerates-api:80/`
  - Volumes: `./bot-data` (отдельная БД), `./bot-logs`
  - Секреты через .env файл

**КРИТИЧНО**: В Docker сети внутренние сервисы обращаются друг к другу по имени контейнера, а не localhost.

## Конфигурация

### ExchangeRates.Api (appsettings.json)

- **ClientConfig:SiteApi/SiteGet**: URL API ЦБ РФ
- **ClientConfig:PeriodMinute**: Интервал фоновых задач (по умолчанию 30)
- **ClientConfig:TimeUpdateJobs**: Время ежедневной задачи (например, "08:40")
- **ClientConfig:JobsValute/JobsValuteToHour**: Boolean флаги включения задач
- **ConnectionStrings:DbData**: SQLite connection string

### ExchangeRatesBot (appsettings.json + переменные окружения)

- **BotConfig:BotToken**: Токен Telegram бота (СЕКРЕТ, использовать .env)
- **BotConfig:UsePolling**: `true` для polling, `false` для webhook (по умолчанию false для обратной совместимости)
- **BotConfig:Webhook**: URL для webhook режима
- **BotConfig:UrlRequest**: URL ExchangeRates.Api (в Docker: `http://exchangerates-api:80/`)
- **BotConfig:TimeOne/TimeTwo**: Время рассылки курсов подписанным пользователям
- **ConnectionStrings:SqliteConnection**: SQLite connection string для БД бота

## Логирование

Обе системы используют **Serilog**:
- Консольный вывод для мониторинга
- SQLite sink (`log.db`) для постоянного хранения
- Настройка в `Program.cs` через `UseSerilog()`

## Особенности реализации

### Telegram.Bot версия и совместимость

**КРИТИЧНО**: Проект использует Telegram.Bot v16.0.2

- **НЕ обновлять** на v17+ из-за breaking changes (extension methods, изменение сигнатур)
- Polling реализован вручную через `GetUpdatesAsync` без Extensions.Polling
- При работе с polling обязательно вызвать `DeleteWebhookAsync()` перед запуском

### SaveService ручной маппинг

`SaveService.SaveSet()` вручную создает 34 объекта `ValuteModelDb` вместо использования рефлексии. Это намеренное решение для типобезопасности и явности, несмотря на многословность кода.

### Background Services

- `BackgroundService` для долгоживущих задач (polling)
- `BackgroundTaskAbstract<T>` для периодических задач по расписанию
- Scoped сервисы в фоновых задачах через `_serviceProvider.CreateScope()`

### Условная регистрация сервисов

В обоих приложениях используется условная регистрация `IHostedService` в `Startup.ConfigureServices`:
```csharp
var config = Configuration.GetSection("Config").Get<Config>();
if (config.EnableFeature)
{
    services.AddHostedService<FeatureService>();
}
```

## .NET версия и совместимость

Проект использует **.NET 5.0** (SDK 10.0+). При создании новых проектов или обновлении зависимостей убедитесь в совместимости с .NET 5.0.

## Источник данных

**API ЦБ РФ**: https://www.cbr-xml-daily.ru/daily_json.js

Возвращает JSON с курсами 34 валют (AMD, AUD, AZN, BGN, BRL, BYN, CAD, CHF, CNY, CZK, DKK, EUR, GBP, HKD, HUF, INR, JPY, KGS, KRW, KZT, MDL, NOK, PLN, RON, SEK, SGD, TJS, TMT, TRY, UAH, USD, UZS, XDR, ZAR). Каждая валюта содержит: `NumCode`, `CharCode`, `Nominal`, `Name`, `Value`, `Previous`, `Id`.
