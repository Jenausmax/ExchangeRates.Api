# CLAUDE.md

Этот файл содержит рекомендации для Claude Code (claude.ai/code) при работе с кодом в этом репозитории.

## Контекст разработчика

Я - Мартин, сеньор .NET разработчик с 20-летним стажем. Работаю на проекте под руководством Макса. Вся коммуникация ведется на русском языке.

## Обзор проекта

ExchangeRates.Api - это ASP.NET Core API для получения и хранения курсов валют от Центрального Банка России (ЦБ РФ). Может работать как самостоятельно, так и в составе системы Telegram-бота. API периодически опрашивает JSON-эндпоинт ЦБ РФ и сохраняет исторические данные о курсах валют в SQLite.

## Команды для сборки и запуска

```bash
# Сборка решения
dotnet build ExchangeRates.Api.sln

# Запуск API
dotnet run --project src/ExchangeRates.Api/ExchangeRates.Api.csproj

# Применение миграций базы данных
dotnet ef database update --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api

# Добавление новой миграции
dotnet ef migrations add <MigrationName> --project src/ExchangeRates.Migrations --startup-project src/ExchangeRates.Api
```

## Архитектура

Решение следует чистой/слоистой архитектуре со следующими проектами:

### Слой ядра (Core Layer)
- **ExchangeRates.Core.Domain**: Доменные модели (`Root`, `Valute`, `ParseModel`) и интерфейсы (`IApiClient`, `IProcessingService`, `ISaveService`, `IGetValute`, `IRepositoryBase<T>`)
- **ExchangeRates.Core.App**: Сервисы приложения, реализующие бизнес-логику
  - `ApiClientService`: Обертка HTTP-клиента для API ЦБ РФ
  - `ProcessingService`: Получает и десериализует JSON от ЦБ РФ
  - `SaveService`: Маппит ответ API в модели БД и сохраняет данные
  - `GetValuteService`: Получает исторические курсы из базы данных

### Инфраструктурный слой (Infrastructure Layer)
- **ExchangeRates.Infrastructure.DB**: EF Core контекст `DataDb` и модели БД (`ValuteModelDb`)
- **ExchangeRates.Infrastructure.SQLite**: Реализация generic-репозитория (`RepositoryDbSQLite<T>`)
- **ExchangeRates.Migrations**: Сборка миграций EF Core

### Слой представления (Presentation Layer)
- **ExchangeRates.Api**: Web API с `ValuteController`, предоставляющим POST-эндпоинт для запроса курсов по коду валюты и диапазону дней

### Вспомогательные проекты
- **ExchangeRates.Configuration**: Класс `ClientConfig` для привязки appsettings
- **ExchangeRates.Maintenance**: Инфраструктура фоновых задач
  - `BackgroundTaskAbstract<T>`: Базовый класс для запланированных задач, выполняющихся каждые `PeriodMinute` минут
  - `JobsCreateValute`: Запланированная задача, выполняющаяся раз в день в `TimeUpdateJobs` (настроенное время)
  - `JobsCreateValuteToHour`: Периодическая задача (каждые 30 мин по умолчанию) с предотвращением дубликатов через `SaveSetNoDublicate`

## Система фоновых задач

Приложение поддерживает две настраиваемые фоновые задачи, управляемые через `appsettings.json`:

1. **JobsValute**: Выполняется в определенное время каждый день (`TimeUpdateJobs`)
2. **JobsValuteToHour**: Выполняется каждые `PeriodMinute` минут с автоматическим определением дубликатов на основе `DateValute`

Задачи условно регистрируются в `Startup.ConfigureServices` на основе boolean-флагов. Обе наследуются от `BackgroundTaskAbstract<T>`, который обрабатывает цикл планирования и разрешение scoped-сервисов.

## Поток данных

1. Фоновая задача срабатывает в настроенный интервал
2. `ProcessingService.RequestProcessing()` вызывает эндпоинт API ЦБ РФ
3. JSON-ответ десериализуется в модель `Root`, содержащую данные `Valute`
4. `SaveService.SaveSet()` или `SaveSetNoDublicate()` вручную маппит каждую валюту в `ValuteModelDb`
5. `RepositoryDbSQLite<T>` сохраняет коллекцию в SQLite через EF Core
6. Эндпоинт контроллера запрашивает репозиторий по параметрам `charCode` и `day`

## Конфигурация

Ключевые настройки в `appsettings.json`:

- **ClientConfig:SiteApi/SiteGet**: Базовый URL API ЦБ РФ и путь эндпоинта
- **ClientConfig:PeriodMinute**: Интервал выполнения фоновых задач (по умолчанию 30 минут)
- **ClientConfig:TimeUpdateJobs**: Точное время для ежедневного выполнения задачи (например, "08:40")
- **ClientConfig:JobsValute/JobsValuteToHour**: Boolean-флаги для включения/отключения каждой фоновой задачи
- **ConnectionStrings:DbData**: Строка подключения к базе данных SQLite

## Логирование

Приложение использует Serilog с двумя приемниками (sinks):
- Вывод в консоль для мониторинга в реальном времени
- База данных SQLite (`log.db`) для постоянных логов

Логирование настраивается в `Program.cs` во время загрузки хоста.

## Источник данных API

Курсы валют получаются из: `https://www.cbr-xml-daily.ru/daily_json.js`

API возвращает JSON-структуру с объектом `Valute`, содержащим свойства 34 валют (AMD, AUD, AZN, BGN, BRL, BYN, CAD, CHF, CNY, CZK, DKK, EUR, GBP, HKD, HUF, INR, JPY, KGS, KRW, KZT, MDL, NOK, PLN, RON, SEK, SGD, TJS, TMT, TRY, UAH, USD, UZS, XDR, ZAR). Каждая валюта включает `NumCode`, `CharCode`, `Nominal`, `Name`, `Value`, `Previous` и `Id`.

## Примечание по реализации SaveService

`SaveService.SaveSet()` вручную создает отдельные объекты `ValuteModelDb` для всех 34 валют вместо использования рефлексии или итерации. Это сделано намеренно для обеспечения явного маппинга и типобезопасности, хотя это приводит к многословному коду.
