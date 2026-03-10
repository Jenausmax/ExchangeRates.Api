- [x] Реализовано

# План добавления Polling режима в Telegram-бота

## Обзор

Добавить поддержку polling режима (опрос Telegram API) в дополнение к существующему webhook режиму. Это позволит боту работать в Docker окружении без необходимости публичного домена и HTTPS сертификата.

## Текущая проблема

Бот работает ТОЛЬКО в webhook режиме:
- Требует публичный HTTPS URL
- В Docker с пустым webhook бот НЕ получает обновления
- Невозможна локальная разработка без ngrok

## Решение

Добавить polling режим с возможностью выбора через конфигурацию (appsettings.json / .env).

## Архитектура

### Текущая:
- **BotService**: Всегда вызывает `SetWebhookAsync()` в конструкторе
- **UpdateController**: POST эндпоинт для webhook
- **CommandService**: Обрабатывает Update (универсален для обоих режимов)

### После изменений:
- **BotService**: Условный вызов `SetWebhookAsync()` или `DeleteWebhookAsync()`
- **PollingBackgroundService**: Новая фоновая задача для polling
- **CommandService**: Без изменений (переиспользуется)
- **UpdateController**: Остается (для webhook режима)

## Ключевые файлы для изменения

### Новые файлы (1):
1. `src/bot/ExchangeRatesBot.Maintenance/Jobs/PollingBackgroundService.cs` - фоновый сервис для polling

### Изменяемые файлы (7):
1. `src/bot/ExchangeRatesBot.Configuration/ModelConfig/BotConfig.cs` - добавить `UsePolling`
2. `src/bot/ExchangeRatesBot.App/Services/BotService.cs` - условная логика
3. `src/bot/ExchangeRatesBot/Startup.cs` - условная регистрация
4. `src/bot/ExchangeRatesBot/appsettings.json` - добавить параметр
5. `src/bot/ExchangeRatesBot.App/ExchangeRatesBot.App.csproj` - добавить NuGet пакет
6. `docker-compose.yml` - добавить переменную окружения
7. `.env` - добавить BOT_USE_POLLING

## План реализации

### Шаг 1: Добавить NuGet пакет Telegram.Bot.Extensions.Polling

**Файл:** `src/bot/ExchangeRatesBot.App/ExchangeRatesBot.App.csproj`

**Действие:** Добавить в ItemGroup:
```xml
<PackageReference Include="Telegram.Bot.Extensions.Polling" Version="1.0.2" />
```

**Команда:**
```bash
cd src/bot/ExchangeRatesBot.App
dotnet add package Telegram.Bot.Extensions.Polling --version 1.0.2
```

---

### Шаг 2: Обновить BotConfig - добавить UsePolling

**Файл:** `src/bot/ExchangeRatesBot.Configuration/ModelConfig/BotConfig.cs`

**Изменение:** Добавить новое свойство:
```csharp
/// <summary>
/// Режим работы бота: true - polling, false - webhook
/// По умолчанию false (webhook режим для обратной совместимости)
/// </summary>
public bool UsePolling { get; set; } = false;
```

**Обоснование:**
- `bool UsePolling` проще чем `string BotMode` ("polling"/"webhook")
- Значение по умолчанию `false` сохраняет обратную совместимость

---

### Шаг 3: Модифицировать BotService - условный webhook

**Файл:** `src/bot/ExchangeRatesBot.App/Services/BotService.cs`

**Текущий код (конструктор):**
```csharp
public BotService(IOptions<BotConfig> config)
{
    _config = config;
    Client = new TelegramBotClient(_config.Value.BotToken);
    Client.SetWebhookAsync(_config.Value.Webhook);
}
```

**Новый код:**
```csharp
private readonly ILogger _logger;

public BotService(IOptions<BotConfig> config, ILogger logger)
{
    _config = config;
    _logger = logger;
    Client = new TelegramBotClient(_config.Value.BotToken);

    // Настройка режима работы бота
    if (_config.Value.UsePolling)
    {
        // Polling режим: удаляем webhook если он был установлен
        Client.DeleteWebhookAsync().Wait();
        _logger.Information("Bot initialized in POLLING mode. Webhook removed.");
    }
    else
    {
        // Webhook режим: устанавливаем webhook URL
        if (string.IsNullOrWhiteSpace(_config.Value.Webhook))
        {
            _logger.Warning("Webhook mode enabled but Webhook URL is empty! Bot may not receive updates.");
        }
        else
        {
            Client.SetWebhookAsync(_config.Value.Webhook).Wait();
            _logger.Information($"Bot initialized in WEBHOOK mode. Webhook set to: {_config.Value.Webhook}");
        }
    }
}
```

**Ключевые изменения:**
- Добавлен `ILogger` в конструктор
- `DeleteWebhookAsync()` вызывается в polling режиме (обязательно!)
- Логирование режима работы
- Проверка пустого Webhook в webhook режиме

---

### Шаг 4: Создать PollingBackgroundService

**Файл:** `src/bot/ExchangeRatesBot.Maintenance/Jobs/PollingBackgroundService.cs` (НОВЫЙ)

**Полный код:**
```csharp
using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ExchangeRatesBot.Maintenance.Jobs
{
    /// <summary>
    /// Фоновый сервис для получения обновлений от Telegram в режиме polling.
    /// Используется для локальной разработки и Docker окружений без публичного домена.
    /// </summary>
    public class PollingBackgroundService : BackgroundService
    {
        private readonly IBotService _botService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly BotConfig _config;

        public PollingBackgroundService(
            IBotService botService,
            IServiceProvider serviceProvider,
            IOptions<BotConfig> config,
            ILogger logger)
        {
            _botService = botService;
            _serviceProvider = serviceProvider;
            _config = config.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Starting Telegram Polling Service...");

            var receiverOptions = new ReceiverOptions
            {
                // Получать все типы обновлений
                AllowedUpdates = Array.Empty<UpdateType>(),

                // Отбросить все накопившиеся обновления при старте
                ThrowPendingUpdates = true
            };

            try
            {
                // StartReceiving НЕ блокирует поток
                _botService.Client.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken
                );

                var me = await _botService.Client.GetMeAsync(stoppingToken);
                _logger.Information($"Polling started for bot @{me.Username} (ID: {me.Id})");

                // Ожидаем сигнала остановки
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Polling service is stopping due to cancellation request.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Critical error in Polling Service");
                throw;
            }
        }

        /// <summary>
        /// Обработчик всех входящих обновлений от Telegram.
        /// Переиспользует CommandService - ту же логику, что использует webhook.
        /// </summary>
        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            // Создаем scope для scoped сервисов
            using var scope = _serviceProvider.CreateScope();

            try
            {
                _logger.Information($"Received update {update.Id} of type {update.Type}");

                // Получаем CommandService из scope
                var commandService = scope.ServiceProvider.GetRequiredService<ICommandBot>();

                // КЛЮЧЕВОЙ МОМЕНТ: Переиспользуем существующую логику
                await commandService.SetCommandBot(update);

                _logger.Information($"Successfully processed update {update.Id}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error processing update {update.Id}");
            }
        }

        /// <summary>
        /// Обработчик ошибок polling.
        /// </summary>
        private Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiEx => $"Telegram API Error [{apiEx.ErrorCode}]: {apiEx.Message}",
                _ => exception.ToString()
            };

            // Логируем с разным уровнем в зависимости от типа ошибки
            if (exception is ApiRequestException)
            {
                _logger.Warning(exception, $"Telegram API error: {errorMessage}");
            }
            else
            {
                _logger.Error(exception, $"Polling error: {errorMessage}");
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stopping Telegram Polling Service...");
            await base.StopAsync(cancellationToken);
            _logger.Information("Telegram Polling Service stopped.");
        }
    }
}
```

**Ключевые моменты:**
- Наследуется от `BackgroundService` (НЕ от BackgroundTaskAbstract - т.к. не периодическая задача)
- `StartReceiving` работает асинхронно, не блокирует поток
- `HandleUpdateAsync` вызывает `commandService.SetCommandBot()` - полное переиспользование логики
- `ReceiverOptions`: `AllowedUpdates = Array.Empty<UpdateType>()` - получать ВСЕ типы
- `ThrowPendingUpdates = true` - очищать старые сообщения при старте

---

### Шаг 5: Обновить Startup.cs - условная регистрация

**Файл:** `src/bot/ExchangeRatesBot/Startup.cs`

**Изменение в методе ConfigureServices:**

**Найти:**
```csharp
services.AddHostedService<JobsSendMessageUsers>();

services.AddControllers().AddNewtonsoftJson();
```

**Заменить на:**
```csharp
services.AddHostedService<JobsSendMessageUsers>();

// Условная регистрация Polling сервиса
var botConfig = Config.GetSection("BotConfig").Get<BotConfig>();
if (botConfig.UsePolling)
{
    services.AddHostedService<PollingBackgroundService>();
}

services.AddControllers().AddNewtonsoftJson();
```

**Обоснование:**
- Polling сервис регистрируется только если `UsePolling = true`
- UpdateController остается (может использоваться в webhook режиме)

---

### Шаг 6: Обновить appsettings.json

**Файл:** `src/bot/ExchangeRatesBot/appsettings.json`

**Найти секцию BotConfig:**
```json
"BotConfig": {
  "BotToken": "718470687:AAF-SsRrPbXWoPyHLo8lIN7aHowpGzjg-Go",
  "Webhook": "https://2fbe3040dfa5.ngrok.io",
  "UrlRequest": "https://a1767-3ec5.f.d-f.pw/",
  "TimeOne": "14:05",
  "TimeTwo": "15:32"
}
```

**Добавить UsePolling:**
```json
"BotConfig": {
  "BotToken": "718470687:AAF-SsRrPbXWoPyHLo8lIN7aHowpGzjg-Go",
  "Webhook": "https://2fbe3040dfa5.ngrok.io",
  "UrlRequest": "https://a1767-3ec5.f.d-f.pw/",
  "TimeOne": "14:05",
  "TimeTwo": "15:32",
  "UsePolling": false
}
```

**Обоснование:**
- `UsePolling: false` по умолчанию - сохраняет обратную совместимость
- Существующие конфигурации продолжат работать в webhook режиме

---

### Шаг 7: Обновить docker-compose.yml

**Файл:** `docker-compose.yml`

**Найти секцию environment для exchangerates-bot:**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:80
  - BotConfig__UrlRequest=http://exchangerates-api:80/
  - BotConfig__BotToken=${BOT_TOKEN}
  - BotConfig__Webhook=${BOT_WEBHOOK:-}
  - BotConfig__TimeOne=${BOT_TIME_ONE:-14:05}
  - BotConfig__TimeTwo=${BOT_TIME_TWO:-15:32}
  - ConnectionStrings__SqliteConnection=Data Source=/app/data/Data.db;foreign keys=false;
```

**Добавить переменную BotConfig__UsePolling:**
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:80
  - BotConfig__UrlRequest=http://exchangerates-api:80/
  - BotConfig__BotToken=${BOT_TOKEN}
  # Режим работы: true = polling (рекомендуется для Docker), false = webhook
  - BotConfig__UsePolling=${BOT_USE_POLLING:-true}
  # Webhook URL (используется только если UsePolling=false)
  - BotConfig__Webhook=${BOT_WEBHOOK:-}
  - BotConfig__TimeOne=${BOT_TIME_ONE:-14:05}
  - BotConfig__TimeTwo=${BOT_TIME_TWO:-15:32}
  - ConnectionStrings__SqliteConnection=Data Source=/app/data/Data.db;foreign keys=false;
```

**Ключевые изменения:**
- Добавлен `BotConfig__UsePolling` с значением по умолчанию `true` (polling для Docker)
- Обновлен комментарий для Webhook

---

### Шаг 8: Обновить .env

**Файл:** `.env`

**Текущее содержимое:**
```env
# Токен Telegram бота (получить от @BotFather)
# ВАЖНО: Заменить на реальный токен перед запуском!
BOT_TOKEN=718470687:AAF-SsRrPbXWoPyHLo8lIN7aHowpGzjg-Go

# Webhook URL (оставить пустым для режима polling)
BOT_WEBHOOK=

# Время рассылки сообщений подписчикам
BOT_TIME_ONE=14:05
BOT_TIME_TWO=15:32
```

**Добавить BOT_USE_POLLING:**
```env
# Токен Telegram бота (получить от @BotFather)
# ВАЖНО: Заменить на реальный токен перед запуском!
BOT_TOKEN=718470687:AAF-SsRrPbXWoPyHLo8lIN7aHowpGzjg-Go

# Режим работы бота
# true = Polling (рекомендуется для локальной разработки и Docker)
# false = Webhook (требует публичный HTTPS URL)
BOT_USE_POLLING=true

# Webhook URL (используется только если BOT_USE_POLLING=false)
# Оставить пустым в polling режиме
BOT_WEBHOOK=

# Время рассылки сообщений подписчикам
BOT_TIME_ONE=14:05
BOT_TIME_TWO=15:32
```

## Команды для запуска

### Локальное тестирование (polling режим)

```bash
# 1. Установить NuGet пакет
cd src/bot/ExchangeRatesBot.App
dotnet add package Telegram.Bot.Extensions.Polling --version 1.0.2

# 2. Запустить приложение
cd ../ExchangeRatesBot
dotnet run
```

**Ожидаемые логи:**
```
[INFO] Bot initialized in POLLING mode. Webhook removed.
[INFO] Starting Telegram Polling Service...
[INFO] Polling started for bot @YourBotName (ID: 123456789)
```

### Docker тестирование

```bash
# 1. Пересобрать образ бота
docker-compose build exchangerates-bot

# 2. Запустить
docker-compose up -d exchangerates-bot

# 3. Проверить логи
docker-compose logs -f exchangerates-bot
```

### Проверка webhook отключен

```bash
# Через Telegram API
curl https://api.telegram.org/bot<YOUR_TOKEN>/getWebhookInfo
```

**Ожидаемый ответ в polling режиме:**
```json
{
  "ok": true,
  "result": {
    "url": "",
    "pending_update_count": 0
  }
}
```

## Тестирование функциональности

### Тест 1: Проверить polling режим работает

1. Запустить бота в polling режиме (`BOT_USE_POLLING=true`)
2. Отправить команду `/start` боту в Telegram
3. Проверить логи:
   ```
   [INFO] Received update 123456 of type Message
   [INFO] Successfully processed update 123456
   ```
4. Убедиться что бот ответил

### Тест 2: Проверить webhook режим работает

1. Изменить `.env`: `BOT_USE_POLLING=false`, `BOT_WEBHOOK=https://your-domain.com/`
2. Перезапустить: `docker-compose restart exchangerates-bot`
3. Проверить логи:
   ```
   [INFO] Bot initialized in WEBHOOK mode. Webhook set to: https://...
   ```
4. Отправить команду боту
5. UpdateController должен получить POST запрос

### Тест 3: Переключение между режимами

1. Запустить в polling режиме
2. Остановить контейнер
3. Изменить `BOT_USE_POLLING=false` в .env
4. Запустить
5. Проверить что webhook установлен через `getWebhookInfo`
6. Повторить в обратном порядке

## Возможные проблемы и решения

### Проблема 1: "Webhook is already set" - polling не работает

**Причина:** DeleteWebhookAsync не сработал

**Решение:**
```bash
# Вручную удалить webhook
curl https://api.telegram.org/bot<TOKEN>/deleteWebhook

# Перезапустить бота
docker-compose restart exchangerates-bot
```

### Проблема 2: Дублирование обновлений

**Причина:** Polling И Webhook работают одновременно

**Решение:**
- Проверить `UsePolling` в конфигурации
- Убедиться что `getWebhookInfo` показывает пустой URL в polling режиме

### Проблема 3: PollingBackgroundService не запускается

**Причина:** UsePolling=false в конфигурации

**Решение:**
- Проверить .env: `BOT_USE_POLLING=true`
- Проверить переменные окружения: `docker exec exchangerates-bot env | grep BOT_USE_POLLING`

## Преимущества polling режима

✅ Не требует публичного домена/IP
✅ Не требует HTTPS сертификата
✅ Проще для локальной разработки
✅ Работает за NAT/firewall
✅ Меньше поверхность атаки (нет открытых портов)

## Когда использовать polling vs webhook

**Polling - рекомендуется для:**
- Локальная разработка
- Docker в закрытой сети
- Тестовые окружения
- Небольшая нагрузка (< 100 запросов/мин)

**Webhook - рекомендуется для:**
- Production с публичным доменом
- Высокая нагрузка (> 1000 запросов/мин)
- Несколько серверов (load balancing)
- Минимальная задержка (мгновенный push)

## Обратная совместимость

- ✅ Существующие конфигурации БЕЗ `UsePolling` продолжат работать (webhook режим)
- ✅ UpdateController остается функциональным
- ✅ CommandService не меняется
- ✅ База данных не затронута
- ✅ Фоновые задачи (JobsSendMessageUsers) не затронуты

## Критические файлы для реализации

1. **PollingBackgroundService.cs** (НОВЫЙ) - центральная логика polling режима
2. **BotService.cs** - условное переключение между режимами
3. **BotConfig.cs** - добавление параметра UsePolling
4. **Startup.cs** - условная регистрация polling сервиса
5. **appsettings.json** - конфигурация UsePolling
6. **docker-compose.yml** - переменная окружения
7. **.env** - значение по умолчанию для Docker

## Последовательность реализации

1. Добавить NuGet пакет Telegram.Bot.Extensions.Polling
2. Обновить BotConfig (добавить UsePolling)
3. Создать PollingBackgroundService
4. Модифицировать BotService (условная логика)
5. Обновить Startup.cs (условная регистрация)
6. Обновить конфигурационные файлы (appsettings, docker-compose, .env)
7. Протестировать локально
8. Протестировать в Docker
9. Проверить переключение между режимами

## Оценка времени реализации

- Изменение кода: 30-40 минут
- Тестирование: 20-30 минут
- Документация: 10-15 минут

**Общее время: ~60-85 минут**
