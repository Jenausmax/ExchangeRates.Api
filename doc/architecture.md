# Архитектурная документация ExchangeRates.Api

**Версия**: 2.0
**Дата**: 2026-03-18
**Автор**: Software Architect Agent

---

## Содержание

1. [Обзор системы](#1-обзор-системы)
2. [Архитектура высокого уровня](#2-архитектура-высокого-уровня)
3. [ExchangeRates.Api -- компонентная архитектура](#3-exchangeratesapi----компонентная-архитектура)
4. [ExchangeRatesBot -- компонентная архитектура](#4-exchangeratesbot----компонентная-архитектура)
5. [NewsService -- компонентная архитектура](#5-newsservice----компонентная-архитектура)
6. [Взаимодействие между сервисами](#6-взаимодействие-между-сервисами)
7. [Потоки данных](#7-потоки-данных)
8. [Режимы работы Telegram-бота (Webhook vs Polling)](#8-режимы-работы-telegram-бота-webhook-vs-polling)
9. [Архитектурные паттерны](#9-архитектурные-паттерны)
10. [Технологический стек](#10-технологический-стек)
11. [Развертывание (Docker Compose)](#11-развертывание-docker-compose)
12. [Архитектурные решения и их обоснование](#12-архитектурные-решения-и-их-обоснование)

---

## 1. Обзор системы

**ExchangeRates.Api** -- система для сбора, хранения и предоставления данных о курсах валют Центрального Банка Российской Федерации. Система состоит из трех микросервисов:

- **ExchangeRates.Api** -- REST API для периодического сбора курсов валют из внешнего источника (ЦБ РФ) и предоставления исторических данных через HTTP-эндпоинт.
- **ExchangeRatesBot** -- Telegram-бот, выступающий клиентом API и предоставляющий пользователям удобный интерфейс для получения курсов валют, подписки на рассылки и новостного дайджеста.
- **NewsService** -- микросервис новостного дайджеста: RSS-парсинг новостей ЦБ РФ, дедупликация, LLM-суммаризация и предоставление дайджеста через HTTP API.

Все сервисы работают как ASP.NET Core Web-приложения на .NET 10.0, развертываются в Docker-контейнерах и взаимодействуют через внутреннюю Docker-сеть.

### Ключевые характеристики

| Характеристика | Значение |
|---|---|
| Платформа | .NET 10.0 (ASP.NET Core) |
| ORM | Entity Framework Core 8.0.0 |
| СУБД | SQLite (отдельные БД для каждого сервиса) |
| Внешний источник курсов | cbr-xml-daily.ru (JSON API ЦБ РФ) |
| Внешний источник новостей | cbr.ru/rss (RSS 2.0 ЦБ РФ) |
| Протокол взаимодействия сервисов | HTTP (REST) |
| Развертывание | Docker Compose (3 контейнера) |
| Логирование | Serilog (Console + SQLite) |
| Поддержка валют | 34 валюты |
| LLM-интеграция | Polza AI, Ollama (опционально) |

---

## 2. Архитектура высокого уровня

### 2.1 Диаграмма системного контекста (C4 Level 1)

```mermaid
graph TB
    User["Пользователь Telegram"]
    TelegramAPI["Telegram Bot API<br/>(api.telegram.org)"]
    Bot["ExchangeRatesBot<br/>(.NET 10.0)"]
    API["ExchangeRates.Api<br/>(.NET 10.0)"]
    News["NewsService<br/>(.NET 10.0)"]
    CBRAPI["API ЦБ РФ<br/>(cbr-xml-daily.ru)"]
    CBRRSS["RSS ЦБ РФ<br/>(cbr.ru/rss)"]
    LLM["LLM Provider<br/>(Polza / Ollama)"]
    APIDb[("SQLite<br/>Data.db<br/>(Курсы валют)")]
    BotDb[("SQLite<br/>Data.db<br/>(Пользователи)")]
    NewsDb[("SQLite<br/>News.db<br/>(Новости)")]

    User -->|"Команды бота"| TelegramAPI
    TelegramAPI -->|"Webhook / Polling"| Bot
    Bot -->|"POST /?charcode=X&day=N"| API
    Bot -->|"GET /api/digest/latest"| News
    API -->|"GET /daily_json.js"| CBRAPI
    News -->|"GET RSS 2.0"| CBRRSS
    News -.->|"Суммаризация (опц.)"| LLM
    API -->|"Чтение/Запись"| APIDb
    Bot -->|"Чтение/Запись"| BotDb
    News -->|"Чтение/Запись"| NewsDb
    Bot -->|"Ответы пользователям"| TelegramAPI
    TelegramAPI -->|"Сообщения"| User

    style API fill:#4a90d9,color:#fff
    style Bot fill:#50b848,color:#fff
    style News fill:#9b59b6,color:#fff
    style CBRAPI fill:#e8a838,color:#fff
    style TelegramAPI fill:#0088cc,color:#fff
```

### 2.2 Диаграмма контейнеров (C4 Level 2)

```mermaid
graph TB
    subgraph Docker["Docker Compose (exchangerates-network)"]
        subgraph APIContainer["exchangerates-api (порт 5000:80)"]
            APIApp["ASP.NET Core Web API"]
            APIBgJobs["Фоновые задачи<br/>JobsCreateValute<br/>JobsCreateValuteToHour"]
        end

        subgraph BotContainer["exchangerates-bot"]
            BotApp["ASP.NET Core Web App"]
            BotPolling["PollingBackgroundService<br/>(опциональный)"]
            BotScheduler["JobsSendMessageUsers<br/>(рассылка курсов)"]
            BotNewsJob["JobsSendNewsDigest<br/>(рассылка новостей)"]
        end

        subgraph NewsContainer["exchangerates-news (порт 5002:80)"]
            NewsApp["ASP.NET Core Web API"]
            NewsFetch["JobsFetchNews<br/>(сбор RSS каждые 60 мин)"]
        end
    end

    subgraph Volumes["Docker Volumes"]
        DataVol["./data:/app/data<br/>(БД API)"]
        LogVol["./logs:/app/logs"]
        BotDataVol["./bot-data:/app/data<br/>(БД бота)"]
        BotLogVol["./bot-logs:/app/logs"]
        NewsDataVol["./news-data:/app/data<br/>(БД новостей)"]
        NewsLogVol["./news-logs:/app/logs"]
    end

    BotApp -->|"HTTP POST<br/>http://exchangerates-api:80/"| APIApp
    BotApp -->|"HTTP GET/POST<br/>http://exchangerates-news:80/"| NewsApp
    APIApp --> DataVol
    APIApp --> LogVol
    BotApp --> BotDataVol
    BotApp --> BotLogVol
    NewsApp --> NewsDataVol
    NewsApp --> NewsLogVol

    style APIContainer fill:#e8f4fd,stroke:#4a90d9
    style BotContainer fill:#e8fde8,stroke:#50b848
    style NewsContainer fill:#f0e6f6,stroke:#9b59b6
```

---

## 3. ExchangeRates.Api -- компонентная архитектура

API-сервис реализован по принципам **Clean Architecture** с разделением на слои.

### 3.1 Структура проектов

```
src/
  ExchangeRates.Api/                    # Presentation Layer (точка входа)
  ExchangeRates.Core.Domain/            # Domain Layer (модели, интерфейсы)
  ExchangeRates.Core.App/               # Application Layer (бизнес-логика)
  ExchangeRates.Infrastructure.DB/      # Infrastructure Layer (EF Core DbContext)
  ExchangeRates.Infrastructure.SQLite/  # Infrastructure Layer (репозиторий SQLite)
  ExchangeRates.Configuration/          # Cross-Cutting (конфигурация)
  ExchangeRates.Maintenance/            # Cross-Cutting (фоновые задачи)
  ExchangeRates.Migrations/             # Infrastructure (EF Core миграции)
```

### 3.2 Компонентная диаграмма ExchangeRates.Api

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        Controller["ValuteController<br/>[ApiController]<br/>POST /?charCode=X&day=N"]
        Startup["Startup<br/>(DI, Middleware)"]
        Program["Program<br/>(Serilog, Host)"]
    end

    subgraph Application["Application Layer (Core.App)"]
        ProcessingService["ProcessingService<br/>: IProcessingService<br/>(Запрос к API ЦБ)"]
        SaveService["SaveService<br/>: ISaveService<br/>(Маппинг и сохранение)"]
        GetValuteService["GetValuteService<br/>: IGetValute<br/>(Чтение истории)"]
        ApiClientService["ApiClientService<br/>: IApiClient<br/>(HTTP-клиент)"]
    end

    subgraph Domain["Domain Layer (Core.Domain)"]
        IProcessingService["IProcessingService"]
        ISaveService["ISaveService"]
        IGetValute["IGetValute"]
        IApiClient["IApiClient"]
        IRepositoryBase["IRepositoryBase T"]
        Root["Root"]
        Valute["Valute<br/>(34 свойства-валюты)"]
        ParseModel["ParseModel<br/>(базовый класс валюты)"]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        DataDb["DataDb<br/>: DbContext<br/>(EF Core)"]
        ValuteModelDb["ValuteModelDb<br/>(DB-модель)"]
        RepositoryDbSQLite["RepositoryDbSQLite T<br/>: IRepositoryBase T"]
    end

    subgraph Maintenance["Maintenance (Фоновые задачи)"]
        BackgroundAbstract["BackgroundTaskAbstract T<br/>: BackgroundService"]
        JobsCreateValute["JobsCreateValute<br/>(ежедневная, по расписанию)"]
        JobsCreateValuteToHour["JobsCreateValuteToHour<br/>(каждые N минут, без дубликатов)"]
    end

    Controller --> IGetValute
    GetValuteService -.->|реализует| IGetValute
    ProcessingService -.->|реализует| IProcessingService
    SaveService -.->|реализует| ISaveService
    ApiClientService -.->|реализует| IApiClient

    ProcessingService --> IApiClient
    SaveService --> IRepositoryBase
    GetValuteService --> IRepositoryBase

    RepositoryDbSQLite -.->|реализует| IRepositoryBase
    RepositoryDbSQLite --> DataDb

    JobsCreateValute --> BackgroundAbstract
    JobsCreateValuteToHour --> BackgroundAbstract
    JobsCreateValute --> IProcessingService
    JobsCreateValute --> ISaveService

    style Presentation fill:#dce8f5,stroke:#4a90d9
    style Application fill:#d5e8d4,stroke:#82b366
    style Domain fill:#fff2cc,stroke:#d6b656
    style Infrastructure fill:#f8cecc,stroke:#b85450
    style Maintenance fill:#e1d5e7,stroke:#9673a6
```

### 3.3 Зависимости между проектами

```mermaid
graph TD
    API["ExchangeRates.Api"] --> CoreApp["ExchangeRates.Core.App"]
    API --> CoreDomain["ExchangeRates.Core.Domain"]
    API --> InfraDB["ExchangeRates.Infrastructure.DB"]
    API --> InfraSQLite["ExchangeRates.Infrastructure.SQLite"]
    API --> Config["ExchangeRates.Configuration"]
    API --> Maintenance["ExchangeRates.Maintenance"]
    API --> Migrations["ExchangeRates.Migrations"]

    CoreApp --> CoreDomain
    CoreApp --> InfraDB
    CoreApp --> Config

    InfraSQLite --> CoreDomain
    InfraSQLite --> InfraDB

    Maintenance --> CoreDomain
    Maintenance --> Config

    Migrations --> InfraDB

    style CoreDomain fill:#fff2cc,stroke:#d6b656
    style CoreApp fill:#d5e8d4,stroke:#82b366
    style API fill:#dce8f5,stroke:#4a90d9
    style InfraDB fill:#f8cecc,stroke:#b85450
    style InfraSQLite fill:#f8cecc,stroke:#b85450
```

> **Примечание**: Зависимость `Core.App --> Infrastructure.DB` (конкретно на `ValuteModelDb`) нарушает строгий принцип Clean Architecture. Это осознанный компромисс ради простоты маппинга в `SaveService`.

---

## 4. ExchangeRatesBot -- компонентная архитектура

Telegram-бот построен по слоистой архитектуре с собственным набором проектов.

### 4.1 Структура проектов

```
src/bot/
  ExchangeRatesBot/                    # Presentation Layer (точка входа, контроллеры)
  ExchangeRatesBot.App/               # Application Layer (сервисы, фразы)
  ExchangeRatesBot.Domain/            # Domain Layer (модели, интерфейсы)
  ExchangeRatesBot.DB/                # Infrastructure Layer (EF Core DbContext, репозиторий)
  ExchangeRatesBot.Configuration/     # Cross-Cutting (конфигурация BotConfig)
  ExchangeRatesBot.Maintenance/       # Cross-Cutting (фоновые задачи, polling)
  ExchangeRatesBot.Migrations/        # Infrastructure (EF Core миграции)
```

### 4.2 Компонентная диаграмма ExchangeRatesBot

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        UpdateController["UpdateController<br/>[ApiController]<br/>POST / (webhook)"]
        BotStartup["Startup<br/>(DI, Middleware)"]
    end

    subgraph Application["Application Layer (Bot.App)"]
        BotService["BotService<br/>: IBotService<br/>(TelegramBotClient, Singleton)"]
        CommandService["CommandService<br/>: ICommandBot<br/>(Маршрутизация команд)"]
        UpdateService["UpdateService<br/>: IUpdateService<br/>(Отправка сообщений)"]
        MessageValute["MessageValuteService<br/>: IMessageValute<br/>(Форматирование курсов)"]
        UserServiceBot["UserService<br/>: IUserService<br/>(Пользователи, подписки)"]
        NewsApiClient["NewsApiClientService<br/>: INewsApiClient<br/>(HTTP-клиент к NewsService)"]
        BotPhrases["BotPhrases<br/>(Статические тексты)"]
    end

    subgraph Domain["Domain Layer (Bot.Domain)"]
        IBotService["IBotService"]
        ICommandBot["ICommandBot"]
        IUserServiceI["IUserService"]
        INewsApiClientI["INewsApiClient"]
        UserModel["User / CurrentUser"]
        Entity["Entity (базовый класс)"]
    end

    subgraph InfrastructureBot["Infrastructure Layer (Bot.DB)"]
        BotDataDb["DataDb<br/>: DbContext"]
        UserDb["UserDb : Entity<br/>(ChatId, Subscribe,<br/>NewsSubscribe, Currencies)"]
        RepositoryDb["RepositoryDb T<br/>: IBaseRepositoryDb T"]
    end

    subgraph MaintenanceBot["Maintenance (Фоновые задачи)"]
        JobsSendMessage["JobsSendMessageUsers<br/>(рассылка курсов по расписанию)"]
        JobsSendNews["JobsSendNewsDigest<br/>(рассылка новостей подписчикам)"]
        PollingService["PollingBackgroundService<br/>(polling режим)"]
    end

    UpdateController --> ICommandBot
    CommandService -.->|реализует| ICommandBot
    BotService -.->|реализует| IBotService
    NewsApiClient -.->|реализует| INewsApiClientI

    CommandService --> IUserServiceI
    CommandService --> INewsApiClientI
    JobsSendNews --> INewsApiClientI

    RepositoryDb --> BotDataDb
    BotDataDb --> UserDb

    style Presentation fill:#dce8f5,stroke:#4a90d9
    style Application fill:#d5e8d4,stroke:#82b366
    style Domain fill:#fff2cc,stroke:#d6b656
    style InfrastructureBot fill:#f8cecc,stroke:#b85450
    style MaintenanceBot fill:#e1d5e7,stroke:#9673a6
```

### 4.3 Команды бота

| Команда | Описание | Обработчик |
|---------|----------|------------|
| `/start` | Запуск бота, отображение Reply-клавиатуры | `CommandService.MessageCommand()` |
| `/help` | Справка по командам | `CommandService.MessageCommand()` |
| `/valuteoneday` | Курсы за сегодня (персональный набор) | `CommandService.MessageCommand()` |
| `/valutesevendays` | Курсы за 7 дней со статистикой | `CommandService.MessageCommand()` |
| `/currencies` | Выбор валют (inline-клавиатура, 10 валют) | `CommandService.MessageCommand()` |
| `/subscribe` | Подписка на рассылку курсов | `CommandService.MessageCommand()` |
| `/unsubscribe` | Отписка от рассылки | `CommandService.MessageCommand()` |
| `/news` | Новостной дайджест (inline-клавиатура) | `CommandService.MessageCommand()` |

**Reply-клавиатура** (3 ряда):
1. `Курс сегодня` | `За 7 дней`
2. `Подписка` | `Помощь`
3. `Новости`

**Inline Callback-запросы**:
- `toggle_{CURRENCY}` -- переключение валюты в /currencies
- `save_currencies` -- сохранение выбора валют
- `news_subscribe` / `news_unsubscribe` -- подписка/отписка на новости
- `news_latest` -- получение последнего дайджеста

---

## 5. NewsService -- компонентная архитектура

Микросервис новостного дайджеста, выделенный из бота для разделения ответственности и независимого масштабирования.

### 5.1 Структура проектов

```
src/newsservice/
  NewsService/                         # Web Host (DigestController, Startup)
  NewsService.App/                     # Сервисы (RSS, LLM, дедупликация, дайджест)
  NewsService.Domain/                  # Модели, DTO, интерфейсы
  NewsService.DB/                      # EF Core контекст NewsDataDb, репозиторий
  NewsService.Configuration/           # NewsConfig, LlmConfig
  NewsService.Maintenance/             # Фоновая задача JobsFetchNews
  NewsService.Migrations/              # EF Core миграции
```

### 5.2 Компонентная диаграмма NewsService

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        DigestController["DigestController<br/>[ApiController]<br/>GET /api/digest/latest<br/>POST /api/digest/mark-sent<br/>GET /api/digest/status"]
        NewsStartup["Startup<br/>(DI, Middleware)"]
    end

    subgraph Application["Application Layer (App)"]
        RssFetcher["RssFetcherService<br/>: IRssFetcherService<br/>(RSS-парсинг ЦБ РФ)"]
        Dedup["NewsDeduplicationService<br/>: INewsDeduplicationService<br/>(SHA256 хеш, LLM)"]
        DigestService["NewsDigestService<br/>: INewsDigestService<br/>(Формирование Markdown)"]
        LlmPolza["PolzaLlmService<br/>: ILlmService"]
        LlmOllama["OllamaLlmService<br/>: ILlmService"]
        LlmNoop["NoopLlmService<br/>: ILlmService<br/>(Fallback)"]
        NormHelper["NewsNormalizationHelper<br/>(Нормализация, хеширование)"]
    end

    subgraph Domain["Domain Layer"]
        IDigest["INewsDigestService"]
        IRss["IRssFetcherService"]
        IDedup["INewsDeduplicationService"]
        ILlm["ILlmService"]
        IRepo["INewsRepository"]
        TopicDb["NewsTopicDb<br/>(Title, Summary, Url,<br/>ContentHash, IsSent)"]
        ItemDb["NewsItemDb<br/>(RawTitle, RawDescription)"]
        DigestDto["DigestResponse<br/>{Message, TopicIds}"]
        StatusDto["ServiceStatusResponse<br/>{TotalTopics, UnsentTopics}"]
    end

    subgraph Infrastructure["Infrastructure Layer (DB)"]
        NewsDataDb["NewsDataDb<br/>: DbContext"]
        NewsRepo["NewsRepository<br/>: INewsRepository"]
    end

    subgraph Maintenance["Maintenance"]
        JobsFetch["JobsFetchNews<br/>(каждые 60 мин)"]
    end

    subgraph Config["Configuration"]
        NewsConfig["NewsConfig<br/>(FetchInterval, MaxNews,<br/>RssFeeds)"]
        LlmConfig["LlmConfig<br/>(Provider, ApiKey,<br/>OllamaUrl)"]
    end

    DigestController --> IDigest
    DigestService -.->|реализует| IDigest
    RssFetcher -.->|реализует| IRss
    Dedup -.->|реализует| IDedup
    LlmPolza -.->|реализует| ILlm
    LlmOllama -.->|реализует| ILlm
    LlmNoop -.->|реализует| ILlm

    DigestService --> IRepo
    Dedup --> IRepo
    Dedup --> ILlm
    Dedup --> NormHelper
    JobsFetch --> IRss
    JobsFetch --> IDedup
    NewsRepo -.->|реализует| IRepo
    NewsRepo --> NewsDataDb

    style Presentation fill:#dce8f5,stroke:#4a90d9
    style Application fill:#d5e8d4,stroke:#82b366
    style Domain fill:#fff2cc,stroke:#d6b656
    style Infrastructure fill:#f8cecc,stroke:#b85450
    style Maintenance fill:#e1d5e7,stroke:#9673a6
    style Config fill:#d5e8d4,stroke:#82b366
```

### 5.3 Зависимости между проектами

```mermaid
graph TD
    Host["NewsService (Host)"] --> App["NewsService.App"]
    Host --> DB["NewsService.DB"]
    Host --> Maint["NewsService.Maintenance"]
    Host --> Migr["NewsService.Migrations"]
    Host --> Conf["NewsService.Configuration"]

    App --> Conf
    App --> DB
    App --> Dom["NewsService.Domain"]

    DB --> Dom
    Maint --> App
    Maint --> Conf
    Maint --> Dom
    Migr --> DB

    style Dom fill:#fff2cc,stroke:#d6b656
    style App fill:#d5e8d4,stroke:#82b366
    style Host fill:#dce8f5,stroke:#4a90d9
    style DB fill:#f8cecc,stroke:#b85450
    style Conf fill:#d5e8d4,stroke:#82b366
```

### 5.4 API эндпоинты NewsService

| Метод | Route | Описание | Тело ответа |
|-------|-------|----------|-------------|
| `GET` | `/api/digest/latest?maxNews=10` | Последний неотправленный дайджест | `DigestResponse { Message, TopicIds }` |
| `POST` | `/api/digest/mark-sent` | Пометить темы как отправленные | `{ MarkedCount }` или `404` |
| `GET` | `/api/digest/status` | Статус сервиса | `ServiceStatusResponse` |

### 5.5 LLM-интеграция (Strategy Pattern)

NewsService поддерживает три LLM-провайдера, выбираемых через конфигурацию `LlmConfig.Provider`:

| Провайдер | Класс | Назначение |
|-----------|-------|------------|
| `polza` | `PolzaLlmService` | Polza AI API (облачный) |
| `ollama` | `OllamaLlmService` | Ollama (локальный) |
| *(пусто)* | `NoopLlmService` | Graceful degradation (без суммаризации) |

LLM используется для:
- Суммаризации новостей (`SummarizeAsync`)
- Определения похожих новостей (`AreSimilarAsync`)

При недоступности LLM система продолжает работать без суммаризации.

### 5.6 RSS-парсинг и дедупликация

```mermaid
flowchart LR
    A["JobsFetchNews<br/>(каждые 60 мин)"] --> B["RssFetcherService<br/>HTTP GET RSS 2.0"]
    B --> C["XmlDocument<br/>парсинг"]
    C --> D["List RssNewsItem"]
    D --> E["NewsDeduplicationService"]
    E --> F{"ContentHash<br/>уже в БД?"}
    F -->|"Да"| G["Пропуск"]
    F -->|"Нет"| H["Сохранить<br/>NewsTopicDb +<br/>NewsItemDb"]
    H --> I{"LLM доступен?"}
    I -->|"Да"| J["Суммаризация<br/>Summary = LLM"]
    I -->|"Нет"| K["Summary = null"]

    style A fill:#e1d5e7,stroke:#9673a6
```

---

## 6. Взаимодействие между сервисами

### 6.1 Сетевая топология в Docker Compose

```mermaid
graph LR
    subgraph DockerNetwork["exchangerates-network (bridge)"]
        API["exchangerates-api<br/>:80"]
        Bot["exchangerates-bot<br/>:80"]
        News["exchangerates-news<br/>:80"]
    end

    Host["Host Machine<br/>:5000 / :5002"]
    TelegramCloud["Telegram API<br/>api.telegram.org"]
    CBR["ЦБ РФ API<br/>cbr-xml-daily.ru"]
    CBRRSS["ЦБ РФ RSS<br/>cbr.ru/rss"]
    LLM["LLM Provider<br/>(Polza / Ollama)"]

    Host -->|"5000:80"| API
    Host -->|"5002:80"| News
    Bot -->|"http://exchangerates-api:80/"| API
    Bot -->|"http://exchangerates-news:80/"| News
    API -->|"HTTPS"| CBR
    News -->|"HTTPS"| CBRRSS
    News -.->|"HTTPS (опц.)"| LLM
    Bot <-->|"HTTPS<br/>(webhook или polling)"| TelegramCloud

    style DockerNetwork fill:#f0f0f0,stroke:#333
```

### 6.2 Диаграмма последовательности: пользователь запрашивает курс валюты

```mermaid
sequenceDiagram
    actor User as Пользователь
    participant TG as Telegram API
    participant Bot as ExchangeRatesBot
    participant CS as CommandService
    participant MV as MessageValuteService
    participant API as ExchangeRates.Api
    participant DB as SQLite (API)

    User->>TG: /valuteoneday
    TG->>Bot: Update (webhook/polling)
    Bot->>CS: SetCommandBot(update)
    CS->>CS: Получить персональный набор валют

    loop для каждой валюты пользователя
        CS->>MV: GetValuteMessage(1, valutes)
        MV->>API: HTTP POST /?charcode=USD&day=1
        API->>DB: SELECT WHERE CharCode = 'USD'
        DB-->>API: ValuteModelDb[]
        API-->>MV: JSON ответ
    end

    MV-->>CS: Отформатированное сообщение со статистикой
    CS->>TG: SendTextMessageAsync(chatId, message)
    TG->>User: Сообщение с курсами
```

### 6.3 Диаграмма последовательности: рассылка новостного дайджеста

```mermaid
sequenceDiagram
    participant Job as JobsSendNewsDigest (Бот)
    participant NAC as NewsApiClientService
    participant News as NewsService API
    participant NDS as NewsDigestService
    participant NDB as SQLite (News)
    participant Bot as BotService
    participant TG as Telegram API

    Job->>Job: Проверка: NewsTime == текущее время?
    Job->>NAC: GetLatestDigestAsync()
    NAC->>News: GET /api/digest/latest
    News->>NDS: GetLatestDigestAsync()
    NDS->>NDB: GetUnsentTopicsAsync()
    NDB-->>NDS: NewsTopicDb[]
    NDS-->>News: DigestResponse {Message, TopicIds}
    News-->>NAC: JSON ответ
    NAC-->>Job: NewsDigestResult

    Job->>Job: Получить подписчиков (NewsSubscribe=true)

    loop для каждого подписчика
        Job->>Bot: SendTextMessageAsync(chatId, digest)
        Bot->>TG: Отправка сообщения
    end

    Job->>NAC: MarkSentAsync(topicIds)
    NAC->>News: POST /api/digest/mark-sent
    News->>NDB: UPDATE SET IsSent=true
```

### 6.4 Контракты HTTP-взаимодействия

**Запрос курсов (Бот --> API):**
```
POST http://exchangerates-api:80/?charcode=USD&day=7
```

**Ответ (API --> Бот):**
```json
{
  "dateGet": "2026-03-18T12:00:00",
  "getValuteModels": [
    {
      "name": "Доллар США",
      "charCode": "USD",
      "value": 92.5,
      "dateSave": "2026-03-18T08:40:00",
      "dateValute": "2026-03-18T00:00:00"
    }
  ]
}
```

**Запрос дайджеста (Бот --> NewsService):**
```
GET http://exchangerates-news:80/api/digest/latest?maxNews=10
```

**Ответ (NewsService --> Бот):**
```json
{
  "message": "Markdown-текст дайджеста...",
  "topicIds": [1, 2, 3]
}
```

---

## 7. Потоки данных

### 7.1 Поток сбора курсов валют

```mermaid
flowchart LR
    A["Таймер<br/>BackgroundTaskAbstract<br/>(каждые N мин)"] --> B["ProcessingService<br/>RequestProcessing()"]
    B --> C["HTTP GET<br/>cbr-xml-daily.ru<br/>/daily_json.js"]
    C --> D["JSON Десериализация<br/>--> Root/Valute"]
    D --> E{"Тип задачи?"}
    E -->|"ежедневная"| F["SaveService.SaveSet()"]
    E -->|"периодическая"| G["SaveService.SaveSetNoDublicate()"]
    G --> H{"Есть запись<br/>с такой датой?"}
    H -->|"Нет"| F
    H -->|"Да"| I["Пропуск записи"]
    F --> J["Маппинг<br/>34 валют --> ValuteModelDb[]"]
    J --> K["SQLite<br/>Data.db"]

    style A fill:#e1d5e7,stroke:#9673a6
    style K fill:#f5a623,color:#fff
```

### 7.2 Поток рассылки курсов подписчикам

```mermaid
flowchart LR
    A["Таймер<br/>(каждую 1 мин)"] --> B{"Текущее время<br/>== TimeOne<br/>или TimeTwo?"}
    B -->|"Нет"| C["Ожидание"]
    B -->|"Да"| D["Группировка<br/>подписчиков<br/>по набору валют"]
    D --> E["Для каждой группы:<br/>MessageValuteService"]
    E --> F["HTTP POST<br/>к ExchangeRates.Api"]
    F --> G["Форматирование<br/>со статистикой"]
    G --> H["SendTextMessageAsync<br/>каждому в группе"]

    style A fill:#e1d5e7,stroke:#9673a6
    style H fill:#0088cc,color:#fff
```

### 7.3 Модель данных

```mermaid
erDiagram
    ValuteModelDb {
        int Id PK
        string ValuteId
        string NumCode
        string CharCode
        int Nominal
        string Name
        double Value
        double Previous
        datetime DateValute
        datetime TimeStampUpdateValute
        datetime DateSave
    }

    UserDb {
        int Id PK
        long ChatId
        string NickName
        string FirstName
        string LastName
        bool Subscribe
        bool NewsSubscribe
        string Currencies "nullable, CSV"
    }

    NewsTopicDb {
        int Id PK
        string Title
        string Summary "nullable, LLM"
        string Url
        string Source
        datetime PublishedAt
        datetime FetchedAt
        bool IsSent
        string ContentHash "unique, SHA256"
    }

    NewsItemDb {
        int Id PK
        int TopicId FK
        string RawTitle
        string RawDescription
        string Link
        datetime PubDate
    }

    NewsTopicDb ||--o{ NewsItemDb : "содержит"
```

**Индексы:**
- `ValuteModelDb`: DateSave, ValuteId, CharCode
- `NewsTopicDb`: ContentHash (unique), IsSent, PublishedAt
- `NewsItemDb`: TopicId (FK)

---

## 8. Режимы работы Telegram-бота (Webhook vs Polling)

### 8.1 Диаграмма сравнения режимов

```mermaid
graph TB
    subgraph WebhookMode["Webhook-режим (UsePolling = false)"]
        TG1["Telegram API"] -->|"HTTP POST<br/>на Webhook URL"| WH["UpdateController<br/>POST /"]
        WH --> CS1["CommandService"]
    end

    subgraph PollingMode["Polling-режим (UsePolling = true)"]
        PBS["PollingBackgroundService"] -->|"GetUpdatesAsync<br/>(long polling, 30s timeout)"| TG2["Telegram API"]
        TG2 -->|"Update[]"| PBS
        PBS --> CS2["CommandService"]
    end

    style WebhookMode fill:#fff3e0,stroke:#e8a838
    style PollingMode fill:#e8f5e9,stroke:#50b848
```

### 8.2 Сравнительная таблица режимов

| Параметр | Webhook | Polling |
|---|---|---|
| Направление соединения | Входящее (Telegram --> бот) | Исходящее (бот --> Telegram) |
| Необходимость публичного IP | Да | Нет |
| HTTPS-сертификат | Обязателен | Не требуется |
| Задержка получения обновлений | Мгновенная | До 30 сек |
| Подходит для Docker | Требует reverse proxy | Да, из коробки |
| Конфигурация | `UsePolling=false`, `Webhook=URL` | `UsePolling=true` |

---

## 9. Архитектурные паттерны

### 9.1 Clean Architecture

Все три сервиса следуют принципам Clean Architecture:

```mermaid
graph TB
    subgraph Layers["Направление зависимостей (внутрь)"]
        P["Presentation<br/>(Controllers, Startup)"]
        A["Application<br/>(Services, бизнес-логика)"]
        D["Domain<br/>(Models, Interfaces)"]
        I["Infrastructure<br/>(DB, Repositories, Migrations)"]
    end

    P --> A
    P --> I
    A --> D
    I --> D

    style D fill:#fff2cc,stroke:#d6b656
    style A fill:#d5e8d4,stroke:#82b366
    style P fill:#dce8f5,stroke:#4a90d9
    style I fill:#f8cecc,stroke:#b85450
```

### 9.2 Repository Pattern

Каждый сервис реализует Generic Repository:
- **API**: `IRepositoryBase<T>` --> `RepositoryDbSQLite<T>`
- **Bot**: `IBaseRepositoryDb<T>` --> `RepositoryDb<T>`
- **News**: `INewsRepository` --> `NewsRepository`

### 9.3 Dependency Injection

Вся регистрация зависимостей в `Startup.ConfigureServices()`:

| API | Lifetime |
|---|---|
| `IApiClient` --> `ApiClientService` | Scoped |
| `IProcessingService` --> `ProcessingService` | Scoped |
| `ISaveService` --> `SaveService` | Scoped |
| `IGetValute` --> `GetValuteService` | Transient |
| `IRepositoryBase<>` --> `RepositoryDbSQLite<>` | Scoped |

| Bot | Lifetime |
|---|---|
| `IBotService` --> `BotService` | **Singleton** |
| `IUpdateService` --> `UpdateService` | Scoped |
| `ICommandBot` --> `CommandService` | Scoped |
| `IMessageValute` --> `MessageValuteService` | Scoped |
| `IUserService` --> `UserService` | Scoped |
| `INewsApiClient` --> `NewsApiClientService` | Scoped |

| NewsService | Lifetime |
|---|---|
| `IRssFetcherService` --> `RssFetcherService` | Scoped |
| `INewsDeduplicationService` --> `NewsDeduplicationService` | Scoped |
| `INewsDigestService` --> `NewsDigestService` | Scoped |
| `INewsRepository` --> `NewsRepository` | Scoped |
| `ILlmService` --> `PolzaLlmService` / `OllamaLlmService` / `NoopLlmService` | Scoped |

### 9.4 Background Service (Hosted Service)

```mermaid
classDiagram
    class BackgroundService {
        <<abstract>>
        +ExecuteAsync(CancellationToken)
    }

    class BackgroundTaskAbstract~T~ {
        <<abstract>>
        -IServiceProvider _services
        +ExecuteAsync(CancellationToken)
        #DoWorkAsync(CancellationToken, IServiceProvider)*
    }

    class NewsBackgroundTask~T~ {
        <<abstract>>
        -NewsConfig _config
        +ExecuteAsync(CancellationToken)
        #DoWorkAsync(CancellationToken, IServiceProvider)*
    }

    class JobsCreateValute {
        "API: ежедневная по расписанию"
    }
    class JobsCreateValuteToHour {
        "API: каждые N минут"
    }
    class JobsSendMessageUsers {
        "Bot: рассылка курсов"
    }
    class JobsSendNewsDigest {
        "Bot: рассылка новостей"
    }
    class JobsFetchNews {
        "News: сбор RSS каждые 60 мин"
    }
    class PollingBackgroundService {
        "Bot: long polling Telegram"
    }

    BackgroundService <|-- BackgroundTaskAbstract
    BackgroundService <|-- NewsBackgroundTask
    BackgroundService <|-- PollingBackgroundService
    BackgroundTaskAbstract <|-- JobsCreateValute
    BackgroundTaskAbstract <|-- JobsCreateValuteToHour
    BackgroundTaskAbstract <|-- JobsSendMessageUsers
    BackgroundTaskAbstract <|-- JobsSendNewsDigest
    NewsBackgroundTask <|-- JobsFetchNews
```

### 9.5 Strategy Pattern (LLM)

NewsService использует Strategy Pattern для выбора LLM-провайдера:

```mermaid
classDiagram
    class ILlmService {
        <<interface>>
        +bool IsAvailable
        +SummarizeAsync(text) string
        +AreSimilarAsync(text1, text2) bool
    }

    class PolzaLlmService {
        "Polza AI API (облачный)"
    }
    class OllamaLlmService {
        "Ollama (локальный)"
    }
    class NoopLlmService {
        "Заглушка (graceful degradation)"
    }

    ILlmService <|.. PolzaLlmService
    ILlmService <|.. OllamaLlmService
    ILlmService <|.. NoopLlmService
```

### 9.6 Условная регистрация сервисов

Все три проекта используют условную регистрацию `HostedService` на основе конфигурации:
- **API**: `JobsCreateValute` при `JobsValute=True`
- **Bot**: `PollingBackgroundService` при `UsePolling=true`, `JobsSendNewsDigest` при `NewsEnabled=true`
- **News**: `JobsFetchNews` при `NewsConfig.Enabled=true`

---

## 10. Технологический стек

| Компонент | Технология | Версия |
|---|---|---|
| Runtime | .NET | 10.0 |
| Web Framework | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 8.0.0 |
| СУБД | SQLite | - |
| Логирование | Serilog | - |
| Serilog Sink (Console) | Serilog.Sinks.Console | - |
| Serilog Sink (SQLite) | Serilog.Sinks.SQLite | - |
| JSON (API) | System.Text.Json | Built-in |
| JSON (Bot) | Newtonsoft.Json | (AddNewtonsoftJson) |
| Telegram SDK | Telegram.Bot | 16.0.2 |
| Контейнеризация | Docker | Multi-stage build |
| Оркестрация | Docker Compose | 3.3 |

---

## 11. Развертывание (Docker Compose)

### 11.1 Диаграмма развертывания

```mermaid
graph TB
    subgraph Host["Host Machine"]
        subgraph DockerCompose["docker-compose.yml"]
            subgraph Network["exchangerates-network (bridge)"]
                APIContainer["exchangerates-api<br/>Ports: 5000:80<br/>depends_on: -"]
                BotContainer["exchangerates-bot<br/>depends_on: api, news"]
                NewsContainer["exchangerates-news<br/>Ports: 5002:80<br/>depends_on: -"]
            end
        end

        subgraph Volumes["Persistent Volumes"]
            DataAPI["./data (API DB)"]
            LogsAPI["./logs"]
            DataBot["./bot-data (Bot DB)"]
            LogsBot["./bot-logs"]
            DataNews["./news-data (News DB)"]
            LogsNews["./news-logs"]
        end

        EnvFile[".env<br/>BOT_TOKEN, NEWS_ENABLED,<br/>LLM_PROVIDER, ..."]
    end

    APIContainer --> DataAPI
    APIContainer --> LogsAPI
    BotContainer --> DataBot
    BotContainer --> LogsBot
    NewsContainer --> DataNews
    NewsContainer --> LogsNews
    EnvFile -.-> BotContainer
    EnvFile -.-> NewsContainer
    BotContainer -->|"http://exchangerates-api:80/"| APIContainer
    BotContainer -->|"http://exchangerates-news:80/"| NewsContainer

    style Network fill:#f0f0f0,stroke:#333
    style Volumes fill:#fff3e0,stroke:#e8a838
```

### 11.2 Переменные окружения

См. [README.md](../README.md#переменные-окружения) для полного списка переменных.

### 11.3 Порядок запуска

```mermaid
sequenceDiagram
    participant DC as docker-compose
    participant API as exchangerates-api
    participant News as exchangerates-news
    participant Bot as exchangerates-bot

    DC->>API: Запуск контейнера
    API->>API: Database.Migrate() (автомиграция)
    API->>API: Запуск фоновых задач

    DC->>News: Запуск контейнера
    News->>News: Database.Migrate() (автомиграция)
    News->>News: Запуск JobsFetchNews

    DC->>Bot: Запуск (depends_on: api, news)
    Bot->>Bot: Database.Migrate() (автомиграция)
    Bot->>Bot: BotService: инициализация TelegramBotClient
    Bot->>Bot: DeleteWebhookAsync() / SetWebhookAsync()
    Bot->>Bot: Запуск PollingBackgroundService (если polling)
    Bot->>Bot: Запуск JobsSendMessageUsers
    Bot->>Bot: Запуск JobsSendNewsDigest (если NewsEnabled)
```

### 11.4 Multi-stage Docker Build

Все три Dockerfile используют двухэтапную сборку:

1. **Build stage** (`mcr.microsoft.com/dotnet/sdk:10.0`): Restore, сборка, публикация
2. **Runtime stage** (`mcr.microsoft.com/dotnet/aspnet:10.0`): Минимальный образ для запуска

---

## 12. Архитектурные решения и их обоснование

### ADR-001: SQLite как СУБД

**Решение**: SQLite для всех трех сервисов.
**Обоснование**: Минимальные требования к инфраструктуре. Данные курсов -- append-only с низким объемом. Каждый сервис использует отдельную БД в отдельном Docker volume.
**Риски**: При росте нагрузки миграция на PostgreSQL через замену `UseSqlite()` на `UseNpgsql()`.

### ADR-002: Раздельные базы данных для каждого сервиса

**Решение**: Три отдельные SQLite базы (Data.db, Data.db, News.db) в раздельных Docker volumes.
**Обоснование**: Разделение ответственности. Устраняет конкурентный доступ к SQLite.

### ADR-003: Telegram.Bot v16.0.2

**Решение**: Фиксированная версия, не обновлять до v17+.
**Обоснование**: v17+ содержит критические breaking changes (переименование методов, изменение сигнатур). Обновление потребует рефакторинга всех мест отправки сообщений.

### ADR-004: Polling как режим по умолчанию

**Решение**: `UsePolling=true` по умолчанию в docker-compose.
**Обоснование**: Docker-окружения часто за NAT. Polling не требует публичного домена.

### ADR-005: Явный маппинг 34 валют

**Решение**: Ручное создание `ValuteModelDb` для каждой валюты в `SaveService.SaveSet()`.
**Обоснование**: Типобезопасность. API ЦБ использует именованные свойства (не коллекцию).

### ADR-006: Scoped сервисы через CreateScope()

**Решение**: Фоновые задачи создают DI-scope через `IServiceProvider.CreateScope()`.
**Обоснование**: BackgroundService -- Singleton, а EF Core DbContext требует Scoped lifetime.

### ADR-007: NewsService как отдельный микросервис

**Решение**: Выделить новостную функциональность в отдельный сервис вместо встраивания в бота.
**Обоснование**: Разделение ответственности (SRP). Независимое масштабирование. Изоляция LLM-нагрузки. Бот обращается к NewsService через HTTP, как к API.

### ADR-008: LLM с graceful degradation

**Решение**: Три реализации ILlmService (Polza, Ollama, Noop) с автоматическим fallback на NoopLlmService.
**Обоснование**: LLM -- опциональная функция. Система должна работать без LLM, просто без суммаризации новостей.

### ADR-009: SHA256 для дедупликации новостей

**Решение**: ContentHash (SHA256 от нормализованного заголовка) с уникальным индексом в БД.
**Обоснование**: Быстрая проверка дубликатов без полнотекстового сравнения. Нормализация (lowercase, trim, strip HTML) уменьшает ложные различия.

### ADR-010: Персонализация набора валют

**Решение**: Поле `Currencies` (CSV-строка, nullable) в UserDb. Inline-клавиатура с 10 валютами.
**Обоснование**: Не все 34 валюты нужны каждому пользователю. NULL = дефолтный набор (USD, EUR, GBP, JPY, CNY) для обратной совместимости.

### ADR-011: Миграция на .NET 10.0 с EF Core 8.0

**Решение**: Обновить все 22 проекта до .NET 10.0, EF Core до 8.0.0. Telegram.Bot остается на v16.0.2.
**Обоснование**: Актуальная платформа, LTS-поддержка. EF Core 8.0.0 -- стабильная версия, совместимая с .NET 10.0.
