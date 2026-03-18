# ExchangeRates.Api

Система для сбора, хранения и предоставления данных о курсах валют Центрального Банка Российской Федерации. Включает REST API, Telegram-бота и новостной сервис с RSS-парсингом.

## Архитектура

Монорепозиторий содержит три микросервиса:

| Сервис | Описание | Порт |
|--------|----------|------|
| **ExchangeRates.Api** | REST API курсов валют ЦБ РФ | 5000 |
| **ExchangeRatesBot** | Telegram-бот для пользователей | - |
| **NewsService** | Новостной дайджест (RSS + LLM) | 5002 |

Все сервисы — ASP.NET Core (.NET 10.0), используют SQLite (каждый свою БД), развертываются через Docker Compose в общей сети.

```
src/
  ExchangeRates.Api/                    # REST API курсов валют (8 проектов)
  bot/ExchangeRatesBot/                 # Telegram-бот (7 проектов)
  newsservice/NewsService/              # Новостной сервис (7 проектов)
```

Подробная архитектурная документация: [doc/architecture.md](doc/architecture.md)

## Быстрый старт (Docker)

### Автоматическое развертывание

```bash
chmod +x deploy.sh && ./deploy.sh
```

Скрипт проверит зависимости, создаст `.env`, соберет и запустит все контейнеры.

### Ручной запуск

1. Создайте файл `.env` из шаблона:

```bash
cp .env.example .env
```

2. Укажите токен Telegram-бота (получить у [@BotFather](https://t.me/BotFather)):

```bash
# .env
BOT_TOKEN=123456789:ABCdefGhIjKlMnOpQrStUvWxYz
```

3. Запустите сервисы:

```bash
docker-compose up -d
```

4. Проверьте статус:

```bash
docker-compose ps
docker-compose logs -f
```

## Переменные окружения

Все переменные задаются в файле `.env` в корне проекта. Шаблон: [.env.example](.env.example)

### Telegram-бот

| Переменная | Описание | По умолчанию |
|------------|----------|--------------|
| `BOT_TOKEN` | Токен бота от @BotFather | *обязательно* |
| `BOT_USE_POLLING` | `true` = polling, `false` = webhook | `true` |
| `BOT_WEBHOOK` | URL для webhook (если polling=false) | *(пусто)* |
| `BOT_TIME_ONE` | Время первой рассылки курсов | `14:05` |
| `BOT_TIME_TWO` | Время второй рассылки курсов | `15:32` |

### Новостной сервис

| Переменная | Описание | По умолчанию |
|------------|----------|--------------|
| `NEWS_ENABLED` | Включить новостной дайджест | `false` |
| `NEWS_TIME` | Время рассылки новостей | `09:00` |
| `LLM_PROVIDER` | LLM для суммаризации: `""`, `polza`, `ollama` | *(пусто)* |
| `POLZA_API_KEY` | API-ключ Polza (если LLM_PROVIDER=polza) | *(пусто)* |
| `OLLAMA_URL` | URL сервера Ollama (если LLM_PROVIDER=ollama) | `http://localhost:11434` |

### Внутренние переменные (docker-compose.yml)

Эти переменные задаются непосредственно в `docker-compose.yml`:

**API сервис:**

| Переменная | Описание | Значение |
|------------|----------|----------|
| `ConnectionStrings__DbData` | Строка подключения SQLite | `Data Source=/app/data/Data.db` |
| `ClientConfig__SiteApi` | URL API ЦБ РФ | `https://www.cbr-xml-daily.ru/` |
| `ClientConfig__SiteGet` | Эндпоинт JSON | `daily_json.js` |
| `ClientConfig__PeriodMinute` | Интервал опроса ЦБ (минуты) | `30` |
| `ClientConfig__TimeUpdateJobs` | Время ежедневного обновления | `08:40` |
| `ClientConfig__JobsValute` | Ежедневная задача | `false` |
| `ClientConfig__JobsValuteToHour` | Периодическая задача | `True` |

**Бот:**

| Переменная | Описание | Значение |
|------------|----------|----------|
| `BotConfig__UrlRequest` | URL API в Docker сети | `http://exchangerates-api:80/` |
| `BotConfig__NewsServiceUrl` | URL NewsService в Docker сети | `http://exchangerates-news:80/` |
| `BotConfig__NewsEnabled` | Включить новости в боте | из `NEWS_ENABLED` |
| `BotConfig__NewsTime` | Время рассылки новостей | из `NEWS_TIME` |
| `ConnectionStrings__SqliteConnection` | SQLite бота | `Data Source=/app/data/Data.db` |

**NewsService:**

| Переменная | Описание | Значение |
|------------|----------|----------|
| `ConnectionStrings__NewsDb` | SQLite новостей | `Data Source=/app/data/News.db` |
| `NewsConfig__Enabled` | Включить сбор RSS | `true` |
| `NewsConfig__FetchIntervalMinutes` | Интервал парсинга RSS (минуты) | `60` |
| `NewsConfig__SendTime` | Время отправки дайджеста | `09:00` |
| `NewsConfig__MaxNewsPerDigest` | Макс. новостей в дайджесте | `5` |
| `LlmConfig__Provider` | LLM провайдер | из `LLM_PROVIDER` |
| `LlmConfig__PolzaApiKey` | Ключ Polza | из `POLZA_API_KEY` |
| `LlmConfig__OllamaUrl` | URL Ollama | из `OLLAMA_URL` |

## Команды бота

| Команда | Описание |
|---------|----------|
| `/start` | Запуск бота, отображение клавиатуры |
| `/help` | Справка по командам |
| `/valuteoneday` | Курсы валют за сегодня |
| `/valutesevendays` | Курсы за 7 дней со статистикой |
| `/currencies` | Выбор валют для отслеживания (inline-клавиатура) |
| `/subscribe` | Подписка на рассылку курсов |
| `/unsubscribe` | Отписка от рассылки |
| `/news` | Новостной дайджест ЦБ РФ (подписка, расписание, отписка) |

Reply-клавиатура: **Курс сегодня**, **За 7 дней**, **Подписка**, **Помощь**, **Новости**

Команда `/news` позволяет:
- Получить последние новости
- Настроить персональное расписание рассылки (6 слотов: 06:00, 09:00, 12:00, 15:00, 18:00, 21:00)
- Подписаться/отписаться от новостного дайджеста

## API эндпоинты

### ExchangeRates.Api

```
POST /?charcode={код_валюты}&day={кол-во_дней}
```

Пример: `POST http://localhost:5000/?charcode=USD&day=7`

### NewsService

```
GET  /api/digest/latest?maxNews=10&since=2026-01-01T00:00:00Z  # Дайджест (since опционален)
POST /api/digest/mark-sent            # Пометить темы как отправленные
GET  /api/digest/status               # Статус сервиса
```

## Локальная разработка

```bash
# Сборка всего решения
dotnet build src/ExchangeRates.Api.sln

# Запуск API
dotnet run --project src/ExchangeRates.Api/ExchangeRates.Api.csproj

# Запуск бота
dotnet run --project src/bot/ExchangeRatesBot/ExchangeRatesBot.csproj

# Запуск NewsService
dotnet run --project src/newsservice/NewsService/NewsService.csproj
```

### Docker команды

```bash
docker-compose up -d              # Запустить все сервисы
docker-compose up -d --build      # Пересобрать и запустить
docker-compose down               # Остановить
docker-compose logs -f             # Логи всех сервисов
docker-compose logs -f exchangerates-bot   # Логи бота
```

## Технологический стек

| Компонент | Технология |
|-----------|------------|
| Runtime | .NET 10.0 |
| Web Framework | ASP.NET Core |
| ORM | Entity Framework Core 8.0 |
| СУБД | SQLite |
| Логирование | Serilog (Console + SQLite) |
| Telegram SDK | Telegram.Bot 16.0.2 |
| Контейнеризация | Docker, Docker Compose |
| Источник данных | [cbr-xml-daily.ru](https://www.cbr-xml-daily.ru/) |

## Поддерживаемые валюты

34 валюты ЦБ РФ: AMD, AUD, AZN, BGN, BRL, BYN, CAD, CHF, CNY, CZK, DKK, EUR, GBP, HKD, HUF, INR, JPY, KGS, KRW, KZT, MDL, NOK, PLN, RON, SEK, SGD, TJS, TMT, TRY, UAH, USD, UZS, XDR, ZAR

## Структура проекта

```
ExchangeRates.Api/
├── src/
│   ├── ExchangeRates.Api/                  # Web API (точка входа)
│   ├── ExchangeRates.Core.Domain/          # Доменные модели, интерфейсы
│   ├── ExchangeRates.Core.App/             # Бизнес-логика
│   ├── ExchangeRates.Infrastructure.DB/    # EF Core DbContext
│   ├── ExchangeRates.Infrastructure.SQLite/ # Репозиторий SQLite
│   ├── ExchangeRates.Configuration/        # Конфигурация
│   ├── ExchangeRates.Maintenance/          # Фоновые задачи
│   ├── ExchangeRates.Migrations/           # EF Core миграции
│   ├── bot/
│   │   ├── ExchangeRatesBot/               # Telegram-бот (точка входа)
│   │   ├── ExchangeRatesBot.App/           # Сервисы, команды, фразы
│   │   ├── ExchangeRatesBot.Domain/        # Модели, интерфейсы
│   │   ├── ExchangeRatesBot.DB/            # EF Core, репозиторий
│   │   ├── ExchangeRatesBot.Configuration/ # BotConfig
│   │   ├── ExchangeRatesBot.Maintenance/   # Фоновые задачи, polling
│   │   └── ExchangeRatesBot.Migrations/    # Миграции
│   └── newsservice/
│       ├── NewsService/                     # Web Host, DigestController
│       ├── NewsService.App/                 # RSS, LLM, дедупликация
│       ├── NewsService.Domain/              # Модели, интерфейсы
│       ├── NewsService.DB/                  # EF Core, репозиторий
│       ├── NewsService.Configuration/       # NewsConfig, LlmConfig
│       ├── NewsService.Maintenance/         # Фоновые задачи (RSS fetch)
│       └── NewsService.Migrations/          # Миграции
├── doc/
│   ├── architecture.md                      # Архитектурная документация
│   └── feature/                             # Планы реализованных фич
├── deploy.sh                                # Скрипт автоматического развертывания
├── docker-compose.yml                       # Конфигурация Docker Compose
├── .env.example                             # Шаблон переменных окружения
└── ExchangeRates.Api.sln                    # Solution (22 проекта)
```

## Лицензия

Проект распространяется как есть, без лицензии.
