# BOT-0028: Статистика криптовалют — План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить статистику криптовалют за период (до 6 месяцев). Кнопка «Статистика» показывает inline-меню с выбором: валюты или монеты.

**Architecture:** Кнопка «Статистика» → inline-меню (stats_valute / stats_crypto) → выбор периода → форматированное сообщение. Бот запрашивает историю через `GET /api/crypto/history` у KriptoService. Форматирование аналогично валютной статистике.

**Tech Stack:** .NET 10.0, Telegram.Bot v16.0.2, KriptoService API

**Спека:** `doc/feature/20260402-crypto-statistics-bot-0028.md`

---

## Task 1: KriptoService — расширить retention и лимит API

**Files:**
- Modify: `src/kriptoservice/KriptoService.Configuration/KriptoConfig.cs`
- Modify: `src/kriptoservice/KriptoService/Controllers/CryptoController.cs`

- [ ] **Step 1: Увеличить HistoryRetentionDays**

В `KriptoConfig.cs` изменить:

```csharp
public int HistoryRetentionDays { get; set; } = 180;
```

- [ ] **Step 2: Увеличить лимит hours в CryptoController**

В `CryptoController.cs`, метод `GetHistory`, изменить валидацию:

```csharp
if (hours < 1 || hours > 4320)
    return BadRequest("hours must be between 1 and 4320");
```

- [ ] **Step 3: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/kriptoservice/KriptoService.Configuration/KriptoConfig.cs src/kriptoservice/KriptoService/Controllers/CryptoController.cs
git commit -m "BOT-0028: KriptoService — retention 180 дней, лимит API 4320 часов"
```

---

## Task 2: IKriptoApiClient — добавить метод GetHistoryAsync

**Files:**
- Modify: `src/bot/ExchangeRatesBot.Domain/Interfaces/IKriptoApiClient.cs`
- Modify: `src/bot/ExchangeRatesBot.App/Services/KriptoApiClientService.cs`

- [ ] **Step 1: Добавить DTO CryptoHistoryResult в IKriptoApiClient.cs**

В конец файла `IKriptoApiClient.cs`, после класса `CryptoPriceItem`, добавить:

```csharp
public class CryptoHistoryResult
{
    public string Symbol { get; set; }
    public string Currency { get; set; }
    public List<CryptoHistoryPoint> Points { get; set; } = new();
}

public class CryptoHistoryPoint
{
    public decimal Price { get; set; }
    public DateTime FetchedAt { get; set; }
}
```

- [ ] **Step 2: Добавить метод в интерфейс IKriptoApiClient**

В интерфейс `IKriptoApiClient` добавить:

```csharp
Task<CryptoHistoryResult> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default);
```

- [ ] **Step 3: Реализовать GetHistoryAsync в KriptoApiClientService**

В `KriptoApiClientService.cs` добавить метод:

```csharp
public async Task<CryptoHistoryResult> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default)
{
    try
    {
        var url = $"{_kriptoServiceUrl}api/crypto/history?symbol={symbol}&currency={currency}&hours={hours}";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, cts.Token);
        var response = await _httpClient.GetStringAsync(url, linked.Token);
        var result = JsonConvert.DeserializeObject<CryptoHistoryResult>(response);
        return result ?? new CryptoHistoryResult { Symbol = symbol, Currency = currency };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка получения истории крипто {Symbol}/{Currency}", symbol, currency);
        return new CryptoHistoryResult { Symbol = symbol, Currency = currency };
    }
}
```

- [ ] **Step 4: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.Domain/Interfaces/IKriptoApiClient.cs src/bot/ExchangeRatesBot.App/Services/KriptoApiClientService.cs
git commit -m "BOT-0028: IKriptoApiClient.GetHistoryAsync — получение истории крипто"
```

---

## Task 3: BotPhrases — новые константы

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs`

- [ ] **Step 1: Добавить константы для inline-меню статистики**

В `BotPhrases.cs`, после блока `// --- BOT-0027: Заголовки inline-меню ---`, добавить:

```csharp
// --- BOT-0028: Inline-кнопки в меню «Статистика» ---
public static string BtnStatsValute { get; } = "Статистика валют";
public static string BtnStatsCrypto { get; } = "Статистика монет";
public static string StatsMenuHeader { get; } = "Выберите раздел статистики:";
public static string CryptoStatsEmpty { get; } = "Нет данных за указанный период. Попробуйте выбрать меньший период.";
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs
git commit -m "BOT-0028: BotPhrases — константы для меню статистики"
```

---

## Task 4: StatisticsHandler — inline-меню + статистика крипто

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs`

Это основная задача. StatisticsHandler получает новую зависимость `IKriptoApiClient` и новые методы.

- [ ] **Step 1: Полностью заменить StatisticsHandler**

Заменить содержимое `src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class StatisticsHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IMessageValute _valuteService;
        private readonly IUserService _userService;
        private readonly IBotService _botService;
        private readonly IKriptoApiClient _kriptoClient;

        public StatisticsHandler(
            IUpdateService updateService,
            IMessageValute valuteService,
            IUserService userService,
            IBotService botService,
            IKriptoApiClient kriptoClient)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userService = userService;
            _botService = botService;
            _kriptoClient = kriptoClient;
        }

        /// <summary>
        /// Кнопка «Статистика» / команда /statistics → inline-меню выбора раздела
        /// </summary>
        public async Task HandleStatisticsCommand(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.StatsMenuHeader,
                new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(BotPhrases.BtnStatsValute, "stats_valute"),
                        InlineKeyboardButton.WithCallbackData(BotPhrases.BtnStatsCrypto, "stats_crypto")
                    }
                }));
        }

        /// <summary>
        /// Callback stats_valute → клавиатура периодов валют
        /// </summary>
        public async Task HandleStatsValute(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                "📊 Выберите период для статистики валют:",
                new InlineKeyboardMarkup(ValutePeriodKeyboard()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback stats_crypto → клавиатура периодов крипто
        /// </summary>
        public async Task HandleStatsCrypto(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                "📊 Выберите период для статистики монет:",
                new InlineKeyboardMarkup(CryptoPeriodKeyboard()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback period_{N} → статистика валют за N дней (без изменений)
        /// </summary>
        public async Task HandlePeriodCallback(Update update, int days)
        {
            var currencies = _userService.GetUserCurrencies(_userService.CurrentUser.ChatId);
            var message = await _valuteService.GetValuteStatisticsMessage(days, currencies, CancellationToken.None);

            if (string.IsNullOrEmpty(message))
            {
                await _updateService.EchoTextMessageAsync(
                    update,
                    "⚠️ Нет данных за указанный период. Попробуйте выбрать меньший период (3-7 дней).",
                    default);
            }
            else
            {
                await _updateService.EchoTextMessageAsync(update, message, default);
            }

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback crypto_period_{N} → статистика монет за N дней
        /// </summary>
        public async Task HandleCryptoPeriodCallback(Update update, int days)
        {
            var coins = _userService.GetUserCryptoCoins(_userService.CurrentUser.ChatId);
            var symbols = coins ?? BotPhrases.AvailableCryptoCoins;
            var hours = days * 24;

            var sb = new StringBuilder();
            sb.AppendLine($"📊 *Статистика криптовалют за {FormatPeriod(days)}* (RUB)");
            sb.AppendLine();

            var hasData = false;
            var index = 1;

            foreach (var symbol in symbols)
            {
                var history = await _kriptoClient.GetHistoryAsync(symbol, "RUB", hours, CancellationToken.None);
                if (history?.Points == null || history.Points.Count < 2)
                    continue;

                // Группировка по дням — берём одну запись в день (последнюю)
                var daily = history.Points
                    .GroupBy(p => p.FetchedAt.Date)
                    .Select(g => g.Last())
                    .OrderBy(p => p.FetchedAt)
                    .ToList();

                if (daily.Count < 2)
                    continue;

                hasData = true;
                var first = daily.First();
                var last = daily.Last();
                var min = daily.MinBy(p => p.Price);
                var max = daily.MaxBy(p => p.Price);
                var change = last.Price - first.Price;
                var changePct = first.Price != 0 ? (change / first.Price) * 100 : 0;
                var trend = change > 0 ? "↑" : change < 0 ? "↓" : "→";
                var sign = change >= 0 ? "+" : "";
                var name = BotPhrases.CryptoNames.GetValueOrDefault(symbol, symbol);

                sb.AppendLine($"{index}. *{symbol}* ({name})");
                sb.AppendLine($"   💰 Текущий: {FormatPrice(last.Price)} ₽");
                sb.AppendLine($"   ↑ Макс: {FormatPrice(max.Price)} ₽ ({max.FetchedAt:dd MMM})");
                sb.AppendLine($"   ↓ Мин: {FormatPrice(min.Price)} ₽ ({min.FetchedAt:dd MMM})");
                sb.AppendLine($"   {trend} Изм: {sign}{FormatPrice(Math.Abs(change))} ₽ ({sign}{changePct.ToString("F1", CultureInfo.InvariantCulture)}%)");
                sb.AppendLine();
                index++;
            }

            if (!hasData)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoStatsEmpty, default);
            }
            else
            {
                await _updateService.EchoTextMessageAsync(update, sb.ToString(), default);
            }

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private static List<List<InlineKeyboardButton>> ValutePeriodKeyboard()
        {
            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("3 дня", "period_3"),
                    InlineKeyboardButton.WithCallbackData("7 дней", "period_7"),
                    InlineKeyboardButton.WithCallbackData("14 дней", "period_14")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("21 день", "period_21"),
                    InlineKeyboardButton.WithCallbackData("30 дней", "period_30")
                }
            };
        }

        private static List<List<InlineKeyboardButton>> CryptoPeriodKeyboard()
        {
            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("3 дня", "crypto_period_3"),
                    InlineKeyboardButton.WithCallbackData("7 дней", "crypto_period_7"),
                    InlineKeyboardButton.WithCallbackData("14 дней", "crypto_period_14")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("21 день", "crypto_period_21"),
                    InlineKeyboardButton.WithCallbackData("30 дней", "crypto_period_30")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("2 мес", "crypto_period_60"),
                    InlineKeyboardButton.WithCallbackData("3 мес", "crypto_period_90"),
                    InlineKeyboardButton.WithCallbackData("4 мес", "crypto_period_120")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("5 мес", "crypto_period_150"),
                    InlineKeyboardButton.WithCallbackData("6 мес", "crypto_period_180")
                }
            };
        }

        private static string FormatPeriod(int days)
        {
            if (days <= 30) return $"{days} дней";
            var months = days / 30;
            return $"{months} мес";
        }

        private static string FormatPrice(decimal price)
        {
            if (price >= 1000)
                return price.ToString("N0", CultureInfo.InvariantCulture);
            if (price >= 1)
                return price.ToString("N2", CultureInfo.InvariantCulture);
            return price.ToString("N4", CultureInfo.InvariantCulture);
        }
    }
}
```

- [ ] **Step 2: Собрать**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок

- [ ] **Step 3: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs
git commit -m "BOT-0028: StatisticsHandler — inline-меню + статистика крипто до 6 мес"
```

---

## Task 5: CommandService — маршрутизация новых callbacks

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Services/CommandService.cs`

- [ ] **Step 1: Добавить prefix-проверку crypto_period_ в RouteCallback**

В методе `RouteCallback`, **после** блока `if (callbackData.StartsWith("period_"))` и **перед** `switch (callbackData)`, добавить:

```csharp
if (callbackData.StartsWith("crypto_period_"))
{
    var days = int.Parse(callbackData.Substring(14));
    await _statisticsHandler.HandleCryptoPeriodCallback(update, days);
    return;
}
```

**ВАЖНО:** Эта проверка должна быть **до** `switch (callbackData)` и **до** проверки `crypto_*` (потому что `crypto_period_` не начинается с `crypto_`... на самом деле начинается!). Нужно поставить проверку `crypto_period_` **перед** проверкой `crypto_*`.

Переместить в начало RouteCallback, **самой первой** prefix-проверкой:

```csharp
// BOT-0028: crypto_period_* ПЕРЕД crypto_* (оба начинаются с "crypto_")
if (callbackData.StartsWith("crypto_period_"))
{
    var days = int.Parse(callbackData.Substring(14));
    await _statisticsHandler.HandleCryptoPeriodCallback(update, days);
    return;
}
```

- [ ] **Step 2: Добавить callbacks stats_valute и stats_crypto в switch**

В exact-match switch в RouteCallback, после case `"settings_subscribe":`, добавить:

```csharp
// --- BOT-0028: Callback'и из inline-меню «Статистика» ---
case "stats_valute":
    await _statisticsHandler.HandleStatsValute(update);
    break;
case "stats_crypto":
    await _statisticsHandler.HandleStatsCrypto(update);
    break;
```

- [ ] **Step 3: Собрать**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок

- [ ] **Step 4: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Services/CommandService.cs
git commit -m "BOT-0028: Маршрутизация stats_valute, stats_crypto, crypto_period_*"
```

---

## Task 6: Startup.cs — обновить DI (если нужно)

**Files:**
- Modify: `src/bot/ExchangeRatesBot/Startup.cs`

- [ ] **Step 1: Проверить что StatisticsHandler получит IKriptoApiClient**

StatisticsHandler теперь зависит от `IKriptoApiClient`. Проверить что `IKriptoApiClient` уже зарегистрирован в DI (должен быть — `services.AddScoped<IKriptoApiClient, KriptoApiClientService>()`). StatisticsHandler зарегистрирован как `services.AddScoped<StatisticsHandler>()` — DI автоматически разрешит новую зависимость.

Если сборка прошла без ошибок — DI в порядке, ничего менять не нужно.

- [ ] **Step 2: Собрать, коммит (если были изменения)**

```bash
dotnet build src/ExchangeRates.Api.sln
```

---

## Task 7: Docker-сборка и деплой

**Files:** нет изменений

- [ ] **Step 1: Собрать и запустить**

```bash
docker-compose up -d --build
```

Expected: все 4 контейнера работают

- [ ] **Step 2: Проверить логи**

```bash
docker-compose logs --tail=10 exchangerates-bot
docker-compose logs --tail=10 exchangerates-kripto
```

Expected: оба сервиса запустились без ошибок

---

## Task 8: Ручное тестирование

- [ ] **Step 1: Тестирование потока**

| Действие | Ожидание |
|----------|----------|
| Нажать «Статистика» | Inline-меню: «Статистика валют», «Статистика монет» |
| Нажать «Статистика валют» | Периоды: 3, 7, 14, 21, 30 дней |
| Нажать «3 дня» | Статистика валют за 3 дня |
| Нажать «Статистика» → «Статистика монет» | Периоды: 3, 7, 14, 21, 30 дней + 2-6 мес |
| Нажать «3 дня» (крипто) | Статистика монет за 3 дня |
| Нажать «30 дней» (крипто) | Статистика монет за 30 дней |

- [ ] **Step 2: Обратная совместимость**

| Команда | Ожидание |
|---------|----------|
| `/statistics` | Inline-меню выбора раздела |

- [ ] **Step 3: Финальный коммит**

```bash
git commit -m "BOT-0028: Статистика криптовалют — реализовано"
```
