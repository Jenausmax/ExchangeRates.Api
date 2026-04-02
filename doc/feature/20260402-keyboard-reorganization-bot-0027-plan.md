# BOT-0027: Реорганизация reply-клавиатуры — План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Упростить reply-клавиатуру бота с 9 кнопок (3 ряда) до 5 кнопок (2 ряда), объединив «Курсы» и «Настройки» как зонтичные кнопки с inline-меню.

**Architecture:** Новые reply-кнопки «Курсы» и «Настройки» отправляют inline-меню. Callback'и `rates_valute`, `rates_crypto`, `settings_currencies`, `settings_crypto_coins`, `settings_subscribe` маршрутизируются через CommandService к существующим handler'ам. Старые reply-кнопки удаляются, slash-команды сохраняются.

**Tech Stack:** .NET 10.0, Telegram.Bot v16.0.2

**Спека:** `doc/feature/20260402-keyboard-reorganization-bot-0027.md`

---

## Task 1: Обновить BotPhrases — новые константы и переименования

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs`

- [ ] **Step 1: Добавить новые константы для reply-кнопок и inline-кнопок**

В `BotPhrases.cs` после строки `public static string BtnCurrencies { get; } = "Валюты";` (строка 51) добавить:

```csharp
// --- BOT-0027: Зонтичные reply-кнопки ---
public static string BtnRates { get; } = "Курсы";
public static string BtnSettings { get; } = "Настройки";

// --- BOT-0027: Inline-кнопки в меню «Курсы» ---
public static string BtnRatesValute { get; } = "Курс валют";
public static string BtnRatesCrypto { get; } = "Курс монет";

// --- BOT-0027: Inline-кнопки в меню «Настройки» ---
public static string BtnSettingsCurrencies { get; } = "Настройки валют";
public static string BtnSettingsCryptoCoins { get; } = "Настройки монет";
public static string BtnSettingsSubscribe { get; } = "Подписка";

// --- BOT-0027: Заголовки inline-меню ---
public static string RatesMenuHeader { get; } = "Выберите раздел:";
public static string SettingsMenuHeader { get; } = "Настройки:";
```

- [ ] **Step 2: Обновить HelpMessage**

Заменить текущий `HelpMessage` на:

```csharp
public static string HelpMessage { get; } =
    "*Доступные команды:*\n\r" +
    "Курсы -- курсы валют и криптовалют\n\r" +
    "Новости -- лента последних новостей\n\r" +
    "Статистика -- детальная статистика за период (3-30 дней)\n\r" +
    "Настройки -- настройки валют, монет и подписок\n\r" +
    "Помощь -- это сообщение\n\r\n\r" +
    "_Также доступны команды:_ /valuteoneday, /valutesevendays, /statistics, /currencies, /subscribe, /news, /crypto, /cryptocoins, /help";
```

- [ ] **Step 3: Собрать**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок

- [ ] **Step 4: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs
git commit -m "BOT-0027: BotPhrases — новые константы для зонтичных кнопок и inline-меню"
```

---

## Task 2: Обновить StartHandler — новая reply-клавиатура

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Handlers/StartHandler.cs`

- [ ] **Step 1: Заменить GetMainKeyboard()**

Заменить метод `GetMainKeyboard()` в `StartHandler.cs`:

```csharp
public static ReplyKeyboardMarkup GetMainKeyboard()
{
    return new ReplyKeyboardMarkup(new[]
    {
        new[] { new KeyboardButton(BotPhrases.BtnRates), new KeyboardButton(BotPhrases.BtnNews), new KeyboardButton(BotPhrases.BtnStatistics) },
        new[] { new KeyboardButton(BotPhrases.BtnSettings), new KeyboardButton(BotPhrases.BtnHelp) }
    })
    {
        ResizeKeyboard = true
    };
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/StartHandler.cs
git commit -m "BOT-0027: Reply-клавиатура 2 ряда (Курсы, Новости, Статистика, Настройки, Помощь)"
```

---

## Task 3: Обновить CommandService — маршрутизация «Курсы» и «Настройки»

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Services/CommandService.cs`

**Контекст:** В `RouteMessage` добавить обработку новых reply-кнопок «Курсы» и «Настройки» — они отправляют inline-меню. В `RouteCallback` добавить обработку новых callback'ов `rates_valute`, `rates_crypto`, `settings_currencies`, `settings_crypto_coins`, `settings_subscribe`. Старые reply-кнопки (BtnValuteSevenDays, BtnCurrencies, BtnSubscribe, BtnCrypto, BtnCryptoCoins) удалить из маршрутизации.

- [ ] **Step 1: Добавить using для ReplyMarkups (если отсутствует)**

В начало `CommandService.cs` добавить (если нет):

```csharp
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
```

- [ ] **Step 2: Обновить RouteMessage — добавить «Курсы» и «Настройки», удалить старые**

В методе `RouteMessage`, заменить блок маршрутов. Удалить case'ы для старых reply-кнопок (`BtnValuteSevenDays`, `BtnCurrencies`, `BtnSubscribe`, `BtnCrypto`, `BtnCryptoCoins`). Добавить case'ы для новых.

Новый полный switch в `RouteMessage`:

```csharp
switch (message)
{
    case "/start":
        await _startHandler.HandleStart(update);
        break;
    case "/help":
    case var txt when txt == BotPhrases.BtnHelp:
        await _startHandler.HandleHelp(update);
        break;

    // --- BOT-0027: Зонтичная кнопка «Курсы» → inline-меню ---
    case var txt when txt == BotPhrases.BtnRates:
        await _updateService.EchoTextMessageAsync(
            update,
            BotPhrases.RatesMenuHeader,
            new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(BotPhrases.BtnRatesValute, "rates_valute"),
                    InlineKeyboardButton.WithCallbackData(BotPhrases.BtnRatesCrypto, "rates_crypto")
                }
            }));
        break;

    // --- BOT-0027: Зонтичная кнопка «Настройки» → inline-меню ---
    case var txt when txt == BotPhrases.BtnSettings:
        await _updateService.EchoTextMessageAsync(
            update,
            BotPhrases.SettingsMenuHeader,
            new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(BotPhrases.BtnSettingsCurrencies, "settings_currencies")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(BotPhrases.BtnSettingsCryptoCoins, "settings_crypto_coins")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(BotPhrases.BtnSettingsSubscribe, "settings_subscribe")
                }
            }));
        break;

    // Slash-команды (без изменений — обратная совместимость)
    case "/valuteoneday":
        await _valuteHandler.HandleOneDay(update);
        break;
    case "/valutesevendays":
        await _valuteHandler.HandleSevenDays(update);
        break;
    case "/statistics":
    case var txt when txt == BotPhrases.BtnStatistics:
        await _statisticsHandler.HandleStatisticsCommand(update);
        break;
    case "/currencies":
        await _currenciesHandler.HandleCurrenciesCommand(update);
        break;
    case "/subscribe":
        await _subscriptionHandler.HandleSubscribeCommand(update);
        break;
    case "/news":
    case var txt when txt == BotPhrases.BtnNews:
        await _newsHandler.HandleNewsCommand(update);
        break;
    case "/crypto":
        await _cryptoHandler.HandleCryptoCommand(update, "RUB");
        break;
    case "/cryptocoins":
        await _cryptoHandler.HandleCryptoCoinsCommand(update);
        break;
    default:
        await _updateService.EchoTextMessageAsync(update, BotPhrases.Error, default);
        break;
}
```

- [ ] **Step 3: Обновить RouteCallback — добавить новые callback'и**

В методе `RouteCallback`, в exact-match switch (после всех prefix-проверок), добавить новые case'ы **перед** `case "save_currencies":`:

```csharp
// --- BOT-0027: Callback'и из inline-меню «Курсы» ---
case "rates_valute":
    await _valuteHandler.HandleOneDay(update);
    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
    break;
case "rates_crypto":
    await _cryptoHandler.HandleCryptoCommand(update, "RUB");
    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
    break;

// --- BOT-0027: Callback'и из inline-меню «Настройки» ---
case "settings_currencies":
    await _currenciesHandler.HandleCurrenciesCommand(update);
    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
    break;
case "settings_crypto_coins":
    await _cryptoHandler.HandleCryptoCoinsCommand(update);
    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
    break;
case "settings_subscribe":
    await _subscriptionHandler.HandleSubscribeCommand(update);
    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
    break;
```

- [ ] **Step 4: Собрать**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок

- [ ] **Step 5: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Services/CommandService.cs
git commit -m "BOT-0027: Маршрутизация «Курсы» и «Настройки» (inline-меню + callbacks)"
```

---

## Task 4: Docker-сборка и деплой

**Files:** нет изменений в файлах

- [ ] **Step 1: Собрать и запустить в Docker**

```bash
docker-compose up -d --build
```

Expected: все 4 контейнера собраны и запущены

- [ ] **Step 2: Проверить логи бота**

```bash
docker-compose logs --tail=20 exchangerates-bot
```

Expected: бот запустился без ошибок

- [ ] **Step 3: Коммит (если были правки)**

Если Docker-сборка выявила проблемы и они исправлены — коммит.

---

## Task 5: Ручное тестирование в Telegram

**Files:** нет изменений

- [ ] **Step 1: Тестирование reply-клавиатуры**

Отправить `/start` — проверить что клавиатура 2 ряда:
```
[ Курсы ] [ Новости ] [ Статистика ]
[ Настройки ] [ Помощь ]
```

- [ ] **Step 2: Тестирование «Курсы»**

| Действие | Ожидание |
|----------|----------|
| Нажать «Курсы» | Inline-меню: «Курс валют», «Курс монет» |
| Нажать «Курс валют» | Курсы валют за 1 день |
| Нажать «Курсы» → «Курс монет» | Курсы крипто в RUB |

- [ ] **Step 3: Тестирование «Настройки»**

| Действие | Ожидание |
|----------|----------|
| Нажать «Настройки» | Inline-меню: «Настройки валют», «Настройки монет», «Подписка» |
| Нажать «Настройки валют» | Клавиатура выбора валют (toggle) |
| Нажать «Настройки монет» | Клавиатура выбора криптомонет (toggle) |
| Нажать «Подписка» | Меню подписок |

- [ ] **Step 4: Тестирование обратной совместимости**

| Команда | Ожидание |
|---------|----------|
| `/valuteoneday` | Курсы за 1 день |
| `/valutesevendays` | Курсы за 7 дней |
| `/currencies` | Выбор валют |
| `/cryptocoins` | Выбор криптомонет |
| `/subscribe` | Меню подписок |
| `/crypto` | Курсы крипто |
| `/news` | Лента новостей |
| `/statistics` | Выбор периода |
| `/help` | Текст помощи (обновлённый) |

- [ ] **Step 5: Финальный коммит**

```bash
git add doc/feature/20260402-keyboard-reorganization-bot-0027.md
git commit -m "BOT-0027: Реорганизация reply-клавиатуры — реализовано"
```
