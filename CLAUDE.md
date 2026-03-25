# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Контекст разработчика

Я - Мартин, сеньор .NET разработчик с 20-летним стажем. Работаю на проекте под руководством Макса. Вся коммуникация ведется на русском языке.

## Обзор проекта

Монорепозиторий содержит четыре микросервиса:

1. **ExchangeRates.Api** — REST API для получения и хранения курсов валют от ЦБ РФ
2. **ExchangeRatesBot** — Telegram-бот для предоставления курсов валют пользователям
3. **NewsService** — микросервис новостного дайджеста (RSS-парсинг ЦБ РФ, LLM-суммаризация)
4. **KriptoService** — микросервис курсов криптовалют (CryptoCompare API, топ-10 монет)

Все сервисы — .NET 10.0 / ASP.NET Core, используют SQLite (каждый свою БД) и развертываются через docker-compose (4 контейнера в общей сети).

## Команды для сборки и запуска

### Локальная разработка

```bash
# Сборка решения (29 проектов)
dotnet build src/ExchangeRates.Api.sln

# Запуск API
dotnet run --project src/ExchangeRates.Api/ExchangeRates.Api.csproj

# Запуск Telegram-бота
dotnet run --project src/bot/ExchangeRatesBot/ExchangeRatesBot.csproj

# Запуск NewsService
dotnet run --project src/newsservice/NewsService/NewsService.csproj

# Запуск KriptoService
dotnet run --project src/kriptoservice/KriptoService/KriptoService.csproj

# EF миграции для API
dotnet ef database update --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api
dotnet ef migrations add <Name> --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api

# EF миграции для бота
dotnet ef database update --project src/bot/ExchangeRatesBot.Migrations --startup-project src/bot/ExchangeRatesBot
dotnet ef migrations add <Name> --project src/bot/ExchangeRatesBot.Migrations --startup-project src/bot/ExchangeRatesBot

# EF миграции для NewsService
dotnet ef database update --project src/newsservice/NewsService.Migrations --startup-project src/newsservice/NewsService
dotnet ef migrations add <Name> --project src/newsservice/NewsService.Migrations --startup-project src/newsservice/NewsService

# EF миграции для KriptoService
dotnet ef database update --project src/kriptoservice/KriptoService.Migrations --startup-project src/kriptoservice/KriptoService
dotnet ef migrations add <Name> --project src/kriptoservice/KriptoService.Migrations --startup-project src/kriptoservice/KriptoService
```

### Docker (рекомендуется)

```bash
# Автоматическое развертывание
chmod +x deploy.sh && ./deploy.sh

# Ручной запуск всех 4 сервисов
docker-compose up -d

# Остановка
docker-compose down

# Просмотр логов
docker-compose logs -f
docker-compose logs -f exchangerates-api
docker-compose logs -f exchangerates-bot
docker-compose logs -f exchangerates-news
docker-compose logs -f exchangerates-kripto

# Пересборка и перезапуск
docker-compose up -d --build
```

**ВАЖНО**: Создайте файл `.env` в корне проекта (шаблон: `.env.example`):
```bash
BOT_TOKEN=your_telegram_bot_token
BOT_USE_POLLING=true
BOT_WEBHOOK=
BOT_TIME_ONE=14:05
BOT_TIME_TWO=15:32
NEWS_ENABLED=false
NEWS_TIME=09:00
LLM_PROVIDER=
POLZA_API_KEY=
OLLAMA_URL=http://localhost:11434
CRYPTO_API_KEY=
```

## Процесс работы над фичами

### Git-ветки

**ОБЯЗАТЕЛЬНО**: Перед началом работы над новой фичей создай ветку и переключись на неё:

```bash
git checkout -b feature/BOT-XXXX-краткое-название
```

**Формат именования**: `feature/BOT-{номер}-{краткое-описание}`

Примеры:
- `feature/BOT-0001-nginx`
- `feature/BOT-0012-news-service`
- `feature/BOT-0013-health-checks`

Номер инкрементируется. Вся работа по фиче ведётся в этой ветке, затем мержится в `develop`.

### Журнал передачи дел (Handoff)

**ОБЯЗАТЕЛЬНО**: После завершения работы над фичей (или в конце рабочей сессии) обновить файл `.claude/handoff.md`:

1. Записать что было сделано сегодня (кратко, по пунктам)
2. Записать что запланировано на следующую сессию
3. Зафиксировать идеи, если появились
4. Зафиксировать блокеры, если есть

Формат — по дате, новые записи добавляются сверху. Старые записи не удалять — это история проекта.

### Планирование

1. **Создание плана**: При входе в режим планирования (EnterPlanMode) создавайте файл плана в папке `doc/feature/` с именем `ГГГГММДД-название-фичи.md`

2. **Формат файла плана**:
   - Первая строка — статус: `- [ ] Не реализовано` или `- [x] Реализовано`
   - Далее полное описание плана реализации

3. **Обновление статуса**: После реализации — `- [x] Реализовано`

4. **Документация**: `doc/` — документация, `doc/feature/` — планы фич, `doc/architecture.md` — архитектура

## Архитектура решения

Решение использует Clean Architecture с разделением на три независимых микросервиса. Подробная документация с Mermaid-диаграммами: [doc/architecture.md](doc/architecture.md)

### ExchangeRates.Api (Сервис курсов валют) — 8 проектов

```
src/
  ExchangeRates.Api/                    # Web API (ValuteController)
  ExchangeRates.Core.Domain/            # Модели (Root, Valute), интерфейсы
  ExchangeRates.Core.App/               # ApiClientService, ProcessingService, SaveService, GetValuteService
  ExchangeRates.Infrastructure.DB/      # EF Core контекст DataDb, ValuteModelDb
  ExchangeRates.Infrastructure.SQLite/  # Generic-репозиторий RepositoryDbSQLite<T>
  ExchangeRates.Configuration/          # ClientConfig
  ExchangeRates.Maintenance/            # BackgroundTaskAbstract<T>, JobsCreateValute, JobsCreateValuteToHour
  ExchangeRates.Migrations/             # EF Core миграции
```

**API эндпоинт**: `POST /?charcode={код}&day={дни}` — запрос курсов по коду валюты

**Поток данных**: Таймер → ProcessingService → HTTP GET cbr-xml-daily.ru → JSON → SaveService → SQLite

### ExchangeRatesBot (Telegram-бот) — 7 проектов

```
src/bot/
  ExchangeRatesBot/                    # Web Host, UpdateController (webhook)
  ExchangeRatesBot.App/               # BotService, CommandService, UpdateService, MessageValuteService, UserService, NewsApiClientService, KriptoApiClientService, BotPhrases
  ExchangeRatesBot.Domain/            # Интерфейсы (IBotService, ICommandBot, IUserService, INewsApiClient, IKriptoApiClient), модели
  ExchangeRatesBot.DB/                # EF Core DataDb, UserDb (ChatId, Subscribe, NewsSubscribe, Currencies, CryptoCoins, NewsTimes, LastNewsDeliveredAt), RepositoryDb<T>
  ExchangeRatesBot.Configuration/     # BotConfig (BotToken, UsePolling, Webhook, UrlRequest, NewsServiceUrl, NewsEnabled, NewsTime, KriptoServiceUrl)
  ExchangeRatesBot.Maintenance/       # JobsSendMessageUsers, JobsSendNewsDigest, PollingBackgroundService
  ExchangeRatesBot.Migrations/        # EF Core миграции
```

**Команды бота**: `/start`, `/help`, `/valuteoneday`, `/valutesevendays`, `/currencies`, `/subscribe`, `/unsubscribe`, `/news`, `/crypto`, `/cryptocoins`

**Reply-клавиатура**: 3 ряда — (Курс сегодня | За 7 дней | Статистика), (Валюты | Подписка | Помощь), (Новости | Крипто | Монеты)

**Inline callbacks**: `toggle_{CURRENCY}`, `save_currencies`, `toggle_crypto_{SYMBOL}`, `save_crypto_coins`, `news_subscribe`, `news_unsubscribe`, `news_latest`, `news_schedule`, `toggle_news_{HH}`, `save_news_schedule`, `sub_toggle_rates`, `sub_toggle_important`, `sub_news_menu`, `sub_news_toggle`, `sub_back`, `news_p_{ID}`, `period_{N}`, `crypto_rub`, `crypto_usd`, `crypto_refresh_rub`, `crypto_refresh_usd`

**Режимы работы** (через `BotConfig.UsePolling`):
- **Polling** (true, рекомендуется для Docker): `PollingBackgroundService` → `GetUpdatesAsync` (long polling, 30s)
- **Webhook** (false): Telegram POST → `UpdateController`

### NewsService (Новостной дайджест) — 7 проектов

```
src/newsservice/
  NewsService/                         # Web Host, DigestController (3 эндпоинта)
  NewsService.App/                     # RssFetcherService, NewsDeduplicationService, NewsDigestService, PolzaLlmService, OllamaLlmService, NoopLlmService, NewsNormalizationHelper
  NewsService.Domain/                  # Модели (NewsTopicDb, NewsItemDb, RssNewsItem), DTO, интерфейсы
  NewsService.DB/                      # EF Core NewsDataDb, NewsRepository
  NewsService.Configuration/           # NewsConfig, LlmConfig
  NewsService.Maintenance/             # NewsBackgroundTask<T>, JobsFetchNews (RSS каждые 60 мин)
  NewsService.Migrations/              # EF Core миграции
```

**API эндпоинты**:
- `GET /api/digest/latest?maxNews=10&since=2026-03-18T09:00:00Z` — дайджест (since опционален, без него — неотправленные)
- `POST /api/digest/mark-sent` — пометить темы как отправленные
- `GET /api/digest/status` — статус сервиса

**LLM-провайдеры** (Strategy Pattern, через `LlmConfig.Provider`):
- `polza` → PolzaLlmService (облачный)
- `ollama` → OllamaLlmService (локальный)
- пусто → NoopLlmService (graceful degradation)

**Дедупликация**: SHA256 от нормализованного заголовка, уникальный индекс ContentHash

### KriptoService (Курсы криптовалют) — 7 проектов

```
src/kriptoservice/
  KriptoService/                       # Web Host, CryptoController (3 эндпоинта)
  KriptoService.App/                   # CryptoCompareFetcherService, CryptoService
  KriptoService.Domain/                # Модели (CryptoPriceDb), DTO, интерфейсы
  KriptoService.DB/                    # EF Core KriptoDataDb, CryptoRepository
  KriptoService.Configuration/         # KriptoConfig
  KriptoService.Maintenance/           # KriptoBackgroundTask<T>, JobsFetchCrypto (каждые 5 мин)
  KriptoService.Migrations/            # EF Core миграции
```

**API эндпоинты**:
- `GET /api/crypto/latest?symbols=BTC,ETH&currencies=RUB` — последние курсы
- `GET /api/crypto/history?symbol=BTC&currency=RUB&hours=24` — история за N часов
- `GET /api/crypto/status` — статус сервиса

**Источник данных**: CryptoCompare API (`/data/pricemultifull`), 100K вызовов/мес, прямая поддержка RUB

**Топ-10 монет**: BTC, ETH, SOL, XRP, BNB, USDT, DOGE, ADA, TON, AVAX

**Фоновая задача**: `JobsFetchCrypto` — фетчинг каждые 5 мин, очистка записей старше 30 дней раз в сутки

### Docker Compose архитектура

Четыре сервиса в общей сети `exchangerates-network`:

| Сервис | Контейнер | Порты | Volumes | Зависит от |
|--------|-----------|-------|---------|------------|
| API | exchangerates-api | 5000:80 | ./databases/api-data, ./logs | — |
| Bot | exchangerates-bot | — | ./databases/bot-data, ./bot-logs | api, news, kripto |
| News | exchangerates-news | 5002:80 | ./databases/news-data, ./news-logs | — |
| Kripto | exchangerates-kripto | 5003:80 | ./databases/kripto-data, ./kripto-logs | — |

**КРИТИЧНО**: В Docker сети сервисы обращаются друг к другу по имени контейнера:
- Бот → API: `http://exchangerates-api:80/`
- Бот → News: `http://exchangerates-news:80/`
- Бот → Kripto: `http://exchangerates-kripto:80/`

## Конфигурация

### ExchangeRates.Api (appsettings.json)

- **ClientConfig:SiteApi/SiteGet**: URL API ЦБ РФ (`https://www.cbr-xml-daily.ru/`, `daily_json.js`)
- **ClientConfig:PeriodMinute**: Интервал фоновых задач (по умолчанию 30)
- **ClientConfig:TimeUpdateJobs**: Время ежедневной задачи (например, "08:40")
- **ClientConfig:JobsValute/JobsValuteToHour**: Boolean флаги включения задач
- **ConnectionStrings:DbData**: SQLite connection string

### ExchangeRatesBot (appsettings.json + .env)

- **BotConfig:BotToken**: Токен Telegram бота (СЕКРЕТ, через .env)
- **BotConfig:UsePolling**: `true` для polling, `false` для webhook
- **BotConfig:Webhook**: URL для webhook режима
- **BotConfig:UrlRequest**: URL ExchangeRates.Api (Docker: `http://exchangerates-api:80/`)
- **BotConfig:TimeOne/TimeTwo**: Время рассылки курсов
- **BotConfig:NewsServiceUrl**: URL NewsService (Docker: `http://exchangerates-news:80/`)
- **BotConfig:NewsEnabled**: Включить фоновую рассылку новостей (`true`/`false`)
- **BotConfig:NewsTime**: Дефолтное время для новых подписчиков (например, "09:00"), per-user расписание хранится в UserDb.NewsTimes
- **BotConfig:KriptoServiceUrl**: URL KriptoService (Docker: `http://exchangerates-kripto:80/`)
- **ConnectionStrings:SqliteConnection**: SQLite connection string

### NewsService (appsettings.json + .env)

- **NewsConfig:Enabled**: Включить сбор RSS
- **NewsConfig:FetchIntervalMinutes**: Интервал парсинга RSS (по умолчанию 60)
- **NewsConfig:SendTime**: Время отправки дайджеста
- **NewsConfig:MaxNewsPerDigest**: Макс. новостей в дайджесте (по умолчанию 5)
- **LlmConfig:Provider**: LLM провайдер (`""`, `polza`, `ollama`)
- **LlmConfig:PolzaApiKey**: API-ключ Polza
- **LlmConfig:OllamaUrl**: URL Ollama сервера
- **ConnectionStrings:NewsDb**: SQLite connection string

### KriptoService (appsettings.json + .env)

- **KriptoConfig:Enabled**: Включить фетчинг криптовалют (`true`/`false`)
- **KriptoConfig:FetchIntervalMinutes**: Интервал фетча (по умолчанию 5)
- **KriptoConfig:ApiUrl**: URL CryptoCompare API
- **KriptoConfig:ApiKey**: API-ключ CryptoCompare (опционально, через .env `CRYPTO_API_KEY`)
- **KriptoConfig:Symbols**: Массив символов монет (BTC, ETH, SOL, ...)
- **KriptoConfig:Currencies**: Массив валют (RUB, USD)
- **KriptoConfig:HistoryRetentionDays**: Хранить записи N дней (по умолчанию 30)
- **ConnectionStrings:KriptoDb**: SQLite connection string

## Логирование

Все четыре сервиса используют **Serilog**:
- Консольный вывод для мониторинга
- SQLite sink (`log.db`) для постоянного хранения
- Настройка в `Program.cs` через `UseSerilog()`

## Особенности реализации

### .NET версия и совместимость

Проект использует **.NET 10.0** с **EF Core 8.0.0**. Все 29 проектов в solution.

### Telegram.Bot версия и совместимость

**КРИТИЧНО**: Проект использует Telegram.Bot v16.0.2

- **НЕ обновлять** на v17+ из-за breaking changes (extension methods, изменение сигнатур, переименование методов)
- Polling реализован вручную через `GetUpdatesAsync` без Extensions.Polling
- При работе с polling обязательно вызвать `DeleteWebhookAsync()` перед запуском

### SaveService ручной маппинг

`SaveService.SaveSet()` вручную создает 34 объекта `ValuteModelDb`. Это намеренное решение для типобезопасности.

### Background Services

- `BackgroundService` для долгоживущих задач (polling)
- `BackgroundTaskAbstract<T>` для периодических задач (API, бот)
- `NewsBackgroundTask<T>` для периодических задач (NewsService)
- `KriptoBackgroundTask<T>` для периодических задач (KriptoService)
- Scoped сервисы в фоновых задачах через `_serviceProvider.CreateScope()`

### Условная регистрация сервисов

Во всех четырёх приложениях используется условная регистрация `IHostedService` в `Startup.ConfigureServices`:
- **API**: `JobsCreateValute` при `JobsValute=True`, `JobsCreateValuteToHour` при `JobsValuteToHour=True`
- **Bot**: `PollingBackgroundService` при `UsePolling=true`, `JobsSendNewsDigest` при `NewsEnabled=true`
- **News**: `JobsFetchNews` при `NewsConfig.Enabled=true`
- **Kripto**: `JobsFetchCrypto` при `KriptoConfig.Enabled=true`

### Персонализация валют

Поле `UserDb.Currencies` (CSV-строка, nullable). NULL = дефолтный набор (USD, EUR, GBP, JPY, CNY). Команда `/currencies` — inline-клавиатура с 10 популярными валютами. Рассылка группирует пользователей по набору валют.

### Персонализация криптовалют

Поле `UserDb.CryptoCoins` (CSV-строка, nullable). NULL = все 10 монет (BTC, ETH, SOL, XRP, BNB, USDT, DOGE, ADA, TON, AVAX). Команда `/cryptocoins` или кнопка «Монеты» — inline-клавиатура с toggle. При нажатии «Крипто» показываются только выбранные монеты (передаётся `symbols` в KriptoService API).

### Персонализированное расписание новостей

Поле `UserDb.NewsTimes` (CSV-строка слотов, nullable). NULL = не подписан на новости. Например: `"09:00"`, `"09:00,18:00"`, `"06:00,12:00,21:00"`. Доступные слоты: 06:00, 09:00, 12:00, 15:00, 18:00, 21:00. Команда `/news` → "Настроить расписание" → inline-клавиатура с toggle-кнопками.

Поле `UserDb.LastNewsDeliveredAt` (DateTime?, nullable) — время последней доставки новостей. `JobsSendNewsDigest` каждую минуту проверяет совпадение текущего HH:mm со слотами пользователей и запрашивает персональный дайджест через `GET /api/digest/latest?since=LastNewsDeliveredAt`.

## Источники данных

- **Курсы валют**: https://www.cbr-xml-daily.ru/daily_json.js — JSON с 34 валютами ЦБ РФ
- **Новости ЦБ**: RSS 2.0 с cbr.ru — парсится через XmlDocument в NewsService
- **Курсы криптовалют**: CryptoCompare API (`https://min-api.cryptocompare.com/data/pricemultifull`) — топ-10 монет, RUB/USD
