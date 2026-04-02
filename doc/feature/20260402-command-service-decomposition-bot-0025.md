# BOT-0025: Декомпозиция CommandService на доменные handler'ы — План реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Разбить монолитный `CommandService` (932 строки) на тонкий роутер + 7 доменных handler'ов для соблюдения SRP.

**Architecture:** CommandService остаётся как роутер (~120 строк), реализующий `ICommandBot`. Бизнес-логика выносится в 7 handler'ов (StartHandler, ValuteHandler, CurrenciesHandler, StatisticsHandler, SubscriptionHandler, NewsHandler, CryptoHandler). Три `static ConcurrentDictionary` заменяются на singleton `IUserSelectionState`.

**Tech Stack:** .NET 10.0, Telegram.Bot v16.0.2, EF Core 8.0, SQLite

**ADR:** `doc/architect/adr-bot-0025-command-service-decomposition.md`
**Архитектура:** `doc/architect/bot-0025-command-service-decomposition.md`

---

## Task 1: Создать ветку и инфраструктуру (IUserSelectionState + UserSelectionState)

**Files:**
- Create: `src/bot/ExchangeRatesBot.Domain/Interfaces/IUserSelectionState.cs`
- Create: `src/bot/ExchangeRatesBot.App/Services/UserSelectionState.cs`

- [ ] **Step 1: Создать ветку**

```bash
git checkout develop
git checkout -b feature/BOT-0025-command-service-decomposition
```

- [ ] **Step 2: Создать интерфейс IUserSelectionState**

Файл: `src/bot/ExchangeRatesBot.Domain/Interfaces/IUserSelectionState.cs`

```csharp
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IUserSelectionState
    {
        ConcurrentDictionary<long, HashSet<string>> PendingCurrencies { get; }
        ConcurrentDictionary<long, HashSet<string>> PendingNewsSchedule { get; }
        ConcurrentDictionary<long, HashSet<string>> PendingCryptoCoins { get; }
    }
}
```

- [ ] **Step 3: Создать реализацию UserSelectionState**

Файл: `src/bot/ExchangeRatesBot.App/Services/UserSelectionState.cs`

```csharp
using System.Collections.Concurrent;
using System.Collections.Generic;
using ExchangeRatesBot.Domain.Interfaces;

namespace ExchangeRatesBot.App.Services
{
    public class UserSelectionState : IUserSelectionState
    {
        public ConcurrentDictionary<long, HashSet<string>> PendingCurrencies { get; } = new();
        public ConcurrentDictionary<long, HashSet<string>> PendingNewsSchedule { get; } = new();
        public ConcurrentDictionary<long, HashSet<string>> PendingCryptoCoins { get; } = new();
    }
}
```

- [ ] **Step 4: Собрать решение**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок (файлы создаются, но пока не используются)

- [ ] **Step 5: Коммит**

```bash
git add src/bot/ExchangeRatesBot.Domain/Interfaces/IUserSelectionState.cs src/bot/ExchangeRatesBot.App/Services/UserSelectionState.cs
git commit -m "BOT-0025: IUserSelectionState + UserSelectionState (singleton для ConcurrentDictionary)"
```

---

## Task 2: Создать папку Handlers и StartHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/StartHandler.cs`

**Контекст:** `StartHandler` — самый простой handler. Обрабатывает `/start`, `/help`, кнопку "Помощь". Содержит `GetMainKeyboard()`.

- [ ] **Step 1: Создать StartHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/StartHandler.cs`

```csharp
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class StartHandler
    {
        private readonly IUpdateService _updateService;

        public StartHandler(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        public async Task HandleStart(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.StartMenu + $"\n\r /subscribe - подписка \n\r /currencies - выбор валют \n\r /valuteoneday - курс на сегодня \n\r /valutesevendays - изменения курса за последние 7 дней \n\r\n\r*Используйте кнопки меню внизу чата!*",
                GetMainKeyboard());
        }

        public async Task HandleHelp(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.HelpMessage);
        }

        public static ReplyKeyboardMarkup GetMainKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(BotPhrases.BtnValuteOneDay), new KeyboardButton(BotPhrases.BtnValuteSevenDays), new KeyboardButton(BotPhrases.BtnStatistics) },
                new[] { new KeyboardButton(BotPhrases.BtnCurrencies),     new KeyboardButton(BotPhrases.BtnSubscribe),     new KeyboardButton(BotPhrases.BtnHelp) },
                new[] { new KeyboardButton(BotPhrases.BtnNews), new KeyboardButton(BotPhrases.BtnCrypto), new KeyboardButton(BotPhrases.BtnCryptoCoins) }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
```

- [ ] **Step 2: Собрать решение**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок

- [ ] **Step 3: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Handlers/StartHandler.cs
git commit -m "BOT-0025: StartHandler (/start, /help)"
```

---

## Task 3: Создать ValuteHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/ValuteHandler.cs`

**Контекст:** Обрабатывает `/valuteoneday`, `/valutesevendays`, кнопки "Курс сегодня", "За 7 дней". Зависит от `IUpdateService`, `IMessageValute`, `IUserService`.

- [ ] **Step 1: Создать ValuteHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/ValuteHandler.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;

namespace ExchangeRatesBot.App.Handlers
{
    public class ValuteHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IMessageValute _valuteService;
        private readonly IUserService _userService;

        public ValuteHandler(IUpdateService updateService, IMessageValute valuteService, IUserService userService)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userService = userService;
        }

        public async Task HandleOneDay(Update update)
        {
            var currencies = _userService.GetUserCurrencies(_userService.CurrentUser.ChatId);
            await _updateService.EchoTextMessageAsync(
                update,
                await _valuteService.GetValuteMessage(1, currencies, CancellationToken.None),
                default);
        }

        public async Task HandleSevenDays(Update update)
        {
            var currencies = _userService.GetUserCurrencies(_userService.CurrentUser.ChatId);
            await _updateService.EchoTextMessageAsync(
                update,
                await _valuteService.GetValuteMessage(8, currencies, CancellationToken.None),
                default);
        }
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/ValuteHandler.cs
git commit -m "BOT-0025: ValuteHandler (/valuteoneday, /valutesevendays)"
```

---

## Task 4: Создать StatisticsHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs`

**Контекст:** Обрабатывает `/statistics`, кнопку "Статистика", callback `period_{N}`. Зависит от `IUpdateService`, `IMessageValute`, `IUserService`, `IBotService`.

- [ ] **Step 1: Создать StatisticsHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.Domain.Interfaces;
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

        public StatisticsHandler(IUpdateService updateService, IMessageValute valuteService, IUserService userService, IBotService botService)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userService = userService;
            _botService = botService;
        }

        public async Task HandleStatisticsCommand(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                "📊 Выберите период для статистики:",
                new InlineKeyboardMarkup(PeriodSelectionKeyboard()));
        }

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

        private static List<List<InlineKeyboardButton>> PeriodSelectionKeyboard()
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
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/StatisticsHandler.cs
git commit -m "BOT-0025: StatisticsHandler (/statistics, period_*)"
```

---

## Task 5: Создать CurrenciesHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/CurrenciesHandler.cs`

**Контекст:** Обрабатывает `/currencies`, "Валюты", `toggle_{CODE}`, `save_currencies`. Использует `IUserSelectionState.PendingCurrencies`.

- [ ] **Step 1: Создать CurrenciesHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/CurrenciesHandler.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class CurrenciesHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly IUserSelectionState _state;

        public CurrenciesHandler(IUpdateService updateService, IBotService botService, IUserService userService, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _state = state;
        }

        public async Task HandleCurrenciesCommand(Update update)
        {
            var chatId = _userService.CurrentUser.ChatId;
            var currentCurrencies = _userService.GetUserCurrencies(chatId);
            _state.PendingCurrencies[chatId] = new HashSet<string>(currentCurrencies);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CurrenciesHeader,
                new InlineKeyboardMarkup(CurrenciesKeyboard(chatId)));
        }

        public async Task HandleToggleCurrency(Update update, string currencyCode)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCurrencies.ContainsKey(chatId))
            {
                var currentCurrencies = _userService.GetUserCurrencies(chatId);
                _state.PendingCurrencies[chatId] = new HashSet<string>(currentCurrencies);
            }

            var selection = _state.PendingCurrencies[chatId];
            if (selection.Contains(currencyCode))
                selection.Remove(currencyCode);
            else
                selection.Add(currencyCode);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(CurrenciesKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveCurrencies(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCurrencies.ContainsKey(chatId) || _state.PendingCurrencies[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CurrenciesEmpty, default);
                return;
            }

            var selected = _state.PendingCurrencies[chatId];
            var currenciesString = string.Join(",", selected);
            await _userService.UpdateCurrencies(chatId, currenciesString, CancellationToken.None);

            _state.PendingCurrencies.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CurrenciesSaved + currenciesString,
                default);
        }

        private List<List<InlineKeyboardButton>> CurrenciesKeyboard(long chatId)
        {
            var selected = _state.PendingCurrencies.ContainsKey(chatId)
                ? _state.PendingCurrencies[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var currency in BotPhrases.AvailableCurrencies)
            {
                var isSelected = selected.Contains(currency);
                var label = isSelected ? $"✅ {currency}" : $"⬜ {currency}";
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_{currency}"));

                if (currentRow.Count == 3)
                {
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);

            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", "save_currencies")
            });

            return rows;
        }
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/CurrenciesHandler.cs
git commit -m "BOT-0025: CurrenciesHandler (/currencies, toggle_*, save_currencies)"
```

---

## Task 6: Создать SubscriptionHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/SubscriptionHandler.cs`

**Контекст:** Самый сложный handler — 6 callbacks + 2 меню + legacy. Обрабатывает `/subscribe`, "Подписка", `sub_toggle_rates`, `sub_toggle_important`, `sub_news_menu`, `sub_news_toggle`, `sub_back`, legacy callbacks.

- [ ] **Step 1: Создать SubscriptionHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/SubscriptionHandler.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class SubscriptionHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;

        public SubscriptionHandler(IUpdateService updateService, IBotService botService, IUserService userService)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
        }

        public async Task HandleSubscribeCommand(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.SubscriptionMenuHeader,
                new InlineKeyboardMarkup(SubscriptionMenu()));
        }

        public async Task HandleToggleRates(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;
            var currentRates = _userService.CurrentUser?.Subscribe == true;
            await _userService.SubscribeUpdate(chatId, !currentRates, CancellationToken.None);
            await _userService.SetUser(chatId);
            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                currentRates ? "Подписка на курсы отменена" : "Подписка на курсы оформлена");
        }

        public async Task HandleToggleImportant(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;
            var currentImportant = _userService.CurrentUser?.ImportantNewsSubscribe == true;
            await _userService.ImportantNewsSubscribeUpdate(chatId, !currentImportant, CancellationToken.None);
            await _userService.SetUser(chatId);
            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                currentImportant ? "Подписка на важные новости отменена" : "Подписка на важные новости оформлена");
        }

        public async Task HandleNewsMenu(Update update)
        {
            await _botService.Client.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: BotPhrases.NewsDigestMenuHeader,
                replyMarkup: new InlineKeyboardMarkup(NewsSubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleNewsToggle(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;
            var currentNews = _userService.CurrentUser?.NewsSubscribe == true;
            if (currentNews)
            {
                await _userService.UpdateNewsTimes(chatId, null, CancellationToken.None);
                await _userService.NewsSubscribeUpdate(chatId, false, CancellationToken.None);
            }
            else
            {
                await _userService.UpdateNewsTimes(chatId, "09:00", CancellationToken.None);
                await _userService.NewsSubscribeUpdate(chatId, true, CancellationToken.None);
            }
            await _userService.SetUser(chatId);
            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(NewsSubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                currentNews ? "Подписка на новости отменена" : "Подписка на новости оформлена (09:00)");
        }

        public async Task HandleBack(Update update)
        {
            await _botService.Client.EditMessageTextAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                text: BotPhrases.SubscriptionMenuHeader,
                replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleLegacyCallbacks(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.SubscriptionMenuHeader,
                new InlineKeyboardMarkup(SubscriptionMenu()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> SubscriptionMenu()
        {
            var user = _userService.CurrentUser;
            var ratesStatus = user?.Subscribe == true ? "\u2705" : "\u274C";
            var importantStatus = user?.ImportantNewsSubscribe == true ? "\u2705" : "\u274C";

            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{ratesStatus} Курсы валют", "sub_toggle_rates")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Новостной дайджест \u25B6", "sub_news_menu")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{importantStatus} Важные новости", "sub_toggle_important")
                }
            };
        }

        private List<List<InlineKeyboardButton>> NewsSubscriptionMenu()
        {
            var user = _userService.CurrentUser;
            var newsStatus = user?.NewsSubscribe == true ? "\u2705" : "\u274C";

            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{newsStatus} Подписка", "sub_news_toggle")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Настроить расписание", "news_schedule")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("\u2190 Назад", "sub_back")
                }
            };
        }
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/SubscriptionHandler.cs
git commit -m "BOT-0025: SubscriptionHandler (/subscribe, sub_*, legacy callbacks)"
```

---

## Task 7: Создать NewsHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/NewsHandler.cs`

**Контекст:** Обрабатывает `/news`, "Новости", `news_latest`, `news_p_{id}`, `news_schedule`, `toggle_news_{HH}`, `save_news_schedule`. Использует `IUserSelectionState.PendingNewsSchedule`.

- [ ] **Step 1: Создать NewsHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/NewsHandler.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class NewsHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly INewsApiClient _newsClient;
        private readonly IUserSelectionState _state;

        public NewsHandler(IUpdateService updateService, IBotService botService, IUserService userService, INewsApiClient newsClient, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _newsClient = newsClient;
            _state = state;
        }

        public async Task HandleNewsCommand(Update update)
        {
            var digest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
        }

        public async Task HandleNewsLatest(Update update)
        {
            var digest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleNewsPage(Update update, int beforeId)
        {
            var digest = await _newsClient.GetDigestBeforeIdAsync(beforeId, 5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsNoMore, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
        }

        public async Task HandleScheduleCommand(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;
            var currentTimes = _userService.GetUserNewsTimes(chatId);
            _state.PendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.NewsScheduleHeader,
                new InlineKeyboardMarkup(NewsScheduleKeyboard(chatId)));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleToggleNewsSlot(Update update, string timeSlotKey)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingNewsSchedule.ContainsKey(chatId))
            {
                var currentTimes = _userService.GetUserNewsTimes(chatId);
                _state.PendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
            }

            var fullSlot = timeSlotKey + ":00";
            var selection = _state.PendingNewsSchedule[chatId];
            if (selection.Contains(fullSlot))
                selection.Remove(fullSlot);
            else
                selection.Add(fullSlot);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(NewsScheduleKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveNewsSchedule(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingNewsSchedule.ContainsKey(chatId) || _state.PendingNewsSchedule[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsScheduleEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _state.PendingNewsSchedule[chatId];
            var sortedSlots = selected.OrderBy(s => s).ToArray();
            var newsTimesString = string.Join(",", sortedSlots);
            await _userService.UpdateNewsTimes(chatId, newsTimesString, CancellationToken.None);
            await _userService.NewsSubscribeUpdate(chatId, true, CancellationToken.None);

            _state.PendingNewsSchedule.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.NewsScheduleSaved + newsTimesString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> NewsScheduleKeyboard(long chatId)
        {
            var selected = _state.PendingNewsSchedule.ContainsKey(chatId)
                ? _state.PendingNewsSchedule[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var slot in BotPhrases.AvailableNewsSlots)
            {
                var isSelected = selected.Contains(slot);
                var label = isSelected ? $"✅ {slot}" : slot;
                var slotKey = slot.Substring(0, 2);
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_news_{slotKey}"));

                if (currentRow.Count == 3)
                {
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);

            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Сохранить", "save_news_schedule")
            });

            return rows;
        }
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/NewsHandler.cs
git commit -m "BOT-0025: NewsHandler (/news, news_*, toggle_news_*, save_news_schedule)"
```

---

## Task 8: Создать CryptoHandler

**Files:**
- Create: `src/bot/ExchangeRatesBot.App/Handlers/CryptoHandler.cs`

**Контекст:** Обрабатывает `/crypto`, `/cryptocoins`, "Крипто", "Монеты", `crypto_*`, `toggle_crypto_*`, `save_crypto_coins`. Содержит форматирование цен, Markdown escaping, клавиатуры.

- [ ] **Step 1: Создать CryptoHandler**

Файл: `src/bot/ExchangeRatesBot.App/Handlers/CryptoHandler.cs`

```csharp
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class CryptoHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly IKriptoApiClient _kriptoClient;
        private readonly IUserSelectionState _state;

        public CryptoHandler(IUpdateService updateService, IBotService botService, IUserService userService, IKriptoApiClient kriptoClient, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _kriptoClient = kriptoClient;
            _state = state;
        }

        public async Task HandleCryptoCommand(Update update, string currency)
        {
            var coins = _userService.GetUserCryptoCoins(_userService.CurrentUser.ChatId);
            var symbols = coins != null ? string.Join(",", coins) : null;
            var result = await _kriptoClient.GetLatestPricesAsync(currency, symbols, CancellationToken.None);

            if (result.Prices == null || result.Prices.Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoEmpty, default);
                return;
            }

            var message = FormatCryptoPrices(result, currency);
            var keyboard = CryptoInlineKeyboard(currency);
            await _updateService.EchoTextMessageAsync(update, message, keyboard);
        }

        public async Task HandleCryptoCallback(Update update, string currency)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;
            var coins = _userService.GetUserCryptoCoins(update.CallbackQuery.From.Id);
            var symbols = coins != null ? string.Join(",", coins) : null;
            var result = await _kriptoClient.GetLatestPricesAsync(currency, symbols, CancellationToken.None);

            if (result.Prices == null || result.Prices.Count == 0)
            {
                await _botService.Client.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: BotPhrases.CryptoEmpty);
                return;
            }

            var message = FormatCryptoPrices(result, currency);
            var keyboard = CryptoInlineKeyboard(currency);

            await _botService.Client.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: message,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleCryptoCoinsCommand(Update update)
        {
            var chatId = _userService.CurrentUser.ChatId;
            var currentCoins = _userService.GetUserCryptoCoins(chatId);
            _state.PendingCryptoCoins[chatId] = currentCoins != null
                ? new HashSet<string>(currentCoins)
                : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CryptoCoinsHeader,
                new InlineKeyboardMarkup(CryptoCoinsKeyboard(chatId)));
        }

        public async Task HandleToggleCryptoSymbol(Update update, string symbol)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCryptoCoins.ContainsKey(chatId))
            {
                var currentCoins = _userService.GetUserCryptoCoins(chatId);
                _state.PendingCryptoCoins[chatId] = currentCoins != null
                    ? new HashSet<string>(currentCoins)
                    : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
            }

            var selection = _state.PendingCryptoCoins[chatId];
            if (selection.Contains(symbol))
                selection.Remove(symbol);
            else
                selection.Add(symbol);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(CryptoCoinsKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveCryptoCoins(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCryptoCoins.ContainsKey(chatId) || _state.PendingCryptoCoins[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoCoinsEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _state.PendingCryptoCoins[chatId];
            var coinsString = string.Join(",", selected);
            await _userService.UpdateCryptoCoins(chatId, coinsString, CancellationToken.None);

            _state.PendingCryptoCoins.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CryptoCoinsSaved + coinsString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> CryptoCoinsKeyboard(long chatId)
        {
            var selected = _state.PendingCryptoCoins.ContainsKey(chatId)
                ? _state.PendingCryptoCoins[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var coin in BotPhrases.AvailableCryptoCoins)
            {
                var isSelected = selected.Contains(coin);
                var label = isSelected ? $"\u2705 {coin}" : $"\u2B1C {coin}";
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_crypto_{coin}"));

                if (currentRow.Count == 3)
                {
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);

            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("\u2705 Сохранить", "save_crypto_coins")
            });

            return rows;
        }

        private static string FormatCryptoPrices(CryptoPriceResult result, string currency)
        {
            var currencySign = currency == "RUB" ? "\u20BD" : "$";
            var sb = new StringBuilder();
            sb.AppendLine($"*Курсы криптовалют ({EscapeMarkdown(currency)})*");
            sb.AppendLine($"_{result.FetchedAt:dd.MM.yyyy HH:mm} UTC_");
            sb.AppendLine();

            var index = 1;
            foreach (var item in result.Prices)
            {
                var arrow = item.ChangePct24h >= 0 ? "\U0001F7E2" : "\U0001F534";
                var sign = item.ChangePct24h >= 0 ? "+" : "";
                var name = EscapeMarkdown(BotPhrases.CryptoNames.GetValueOrDefault(item.Symbol ?? "", item.Symbol ?? ""));
                var symbol = EscapeMarkdown(item.Symbol ?? "");
                var priceStr = FormatCryptoPrice(item.Price);

                sb.AppendLine($"{index}. *{symbol}* ({name})");
                sb.AppendLine($"   {priceStr} {currencySign}  {arrow} {sign}{item.ChangePct24h.ToString("F1", CultureInfo.InvariantCulture)}%");
                sb.AppendLine();
                index++;
            }

            return sb.ToString();
        }

        private static string FormatCryptoPrice(decimal price)
        {
            if (price >= 1000)
                return price.ToString("N0", CultureInfo.InvariantCulture);
            if (price >= 1)
                return price.ToString("N2", CultureInfo.InvariantCulture);
            return price.ToString("N4", CultureInfo.InvariantCulture);
        }

        private static string EscapeMarkdown(string text)
            => text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("`", "\\`");

        private static InlineKeyboardMarkup CryptoInlineKeyboard(string activeCurrency)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        activeCurrency == "RUB" ? "[ RUB ]" : "RUB", "crypto_rub"),
                    InlineKeyboardButton.WithCallbackData(
                        activeCurrency == "USD" ? "[ USD ]" : "USD", "crypto_usd")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        "\U0001F504 Обновить", $"crypto_refresh_{activeCurrency.ToLower()}")
                }
            });
        }
    }
}
```

- [ ] **Step 2: Собрать, коммит**

```bash
dotnet build src/ExchangeRates.Api.sln
git add src/bot/ExchangeRatesBot.App/Handlers/CryptoHandler.cs
git commit -m "BOT-0025: CryptoHandler (/crypto, /cryptocoins, crypto_*, toggle_crypto_*, save_crypto_coins)"
```

---

## Task 9: Переписать CommandService в тонкий роутер + обновить DI

**Files:**
- Modify: `src/bot/ExchangeRatesBot.App/Services/CommandService.cs` (полная замена)
- Modify: `src/bot/ExchangeRatesBot/Startup.cs` (добавить DI-регистрацию)

**Контекст:** CommandService теперь ~120 строк — только маршрутизация. Все handler'ы получает через конструктор. `IProcessingService` убирается из конструктора. Static `ConcurrentDictionary` удаляются.

**КРИТИЧНО:** Порядок callback-matching должен быть идентичен оригиналу: `crypto_*` → `news_p_*` → `toggle_crypto_*` → `toggle_news_*` → `toggle_*` → `period_*` → exact switch.

- [ ] **Step 1: Заменить CommandService на роутер**

Полностью заменить содержимое `src/bot/ExchangeRatesBot.App/Services/CommandService.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Handlers;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ExchangeRatesBot.App.Services
{
    public class CommandService : ICommandBot
    {
        private readonly IUpdateService _updateService;
        private readonly IUserService _userControl;
        private readonly IBotService _botService;

        private readonly StartHandler _startHandler;
        private readonly ValuteHandler _valuteHandler;
        private readonly CurrenciesHandler _currenciesHandler;
        private readonly StatisticsHandler _statisticsHandler;
        private readonly SubscriptionHandler _subscriptionHandler;
        private readonly NewsHandler _newsHandler;
        private readonly CryptoHandler _cryptoHandler;

        public CommandService(
            IUpdateService updateService,
            IUserService userControl,
            IBotService botService,
            StartHandler startHandler,
            ValuteHandler valuteHandler,
            CurrenciesHandler currenciesHandler,
            StatisticsHandler statisticsHandler,
            SubscriptionHandler subscriptionHandler,
            NewsHandler newsHandler,
            CryptoHandler cryptoHandler)
        {
            _updateService = updateService;
            _userControl = userControl;
            _botService = botService;
            _startHandler = startHandler;
            _valuteHandler = valuteHandler;
            _currenciesHandler = currenciesHandler;
            _statisticsHandler = statisticsHandler;
            _subscriptionHandler = subscriptionHandler;
            _newsHandler = newsHandler;
            _cryptoHandler = cryptoHandler;
        }

        public async Task SetCommandBot(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    var resMessageUser = await _userControl.SetUser(update.Message.From.Id);
                    if (!resMessageUser)
                    {
                        var user = new Domain.Models.User()
                        {
                            ChatId = update.Message.From.Id,
                            NickName = update.Message.From.Username,
                            Subscribe = false,
                            FirstName = update.Message.From.FirstName,
                            LastName = update.Message.From.LastName
                        };
                        await _userControl.Create(user, CancellationToken.None);
                        await _userControl.SetUser(user.ChatId);
                    }
                    await RouteMessage(update);
                    break;

                case UpdateType.CallbackQuery:
                    await _userControl.SetUser(update.CallbackQuery.From.Id);
                    await RouteCallback(update);
                    break;

                default:
                    await _updateService.EchoTextMessageAsync(update, BotPhrases.Error, default);
                    break;
            }
        }

        private async Task RouteMessage(Update update)
        {
            var message = update.Message.Text;
            if (string.IsNullOrEmpty(message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.Error, default);
                return;
            }

            switch (message)
            {
                case "/start":
                    await _startHandler.HandleStart(update);
                    break;
                case "/help":
                case var txt when txt == BotPhrases.BtnHelp:
                    await _startHandler.HandleHelp(update);
                    break;
                case "/valuteoneday":
                case var txt when txt == BotPhrases.BtnValuteOneDay:
                    await _valuteHandler.HandleOneDay(update);
                    break;
                case "/valutesevendays":
                case var txt when txt == BotPhrases.BtnValuteSevenDays:
                    await _valuteHandler.HandleSevenDays(update);
                    break;
                case "/statistics":
                case var txt when txt == BotPhrases.BtnStatistics:
                    await _statisticsHandler.HandleStatisticsCommand(update);
                    break;
                case "/currencies":
                case var txt when txt == BotPhrases.BtnCurrencies:
                    await _currenciesHandler.HandleCurrenciesCommand(update);
                    break;
                case "/subscribe":
                case var txt when txt == BotPhrases.BtnSubscribe:
                    await _subscriptionHandler.HandleSubscribeCommand(update);
                    break;
                case "/news":
                case var txt when txt == BotPhrases.BtnNews:
                    await _newsHandler.HandleNewsCommand(update);
                    break;
                case "/crypto":
                case var txt when txt == BotPhrases.BtnCrypto:
                    await _cryptoHandler.HandleCryptoCommand(update, "RUB");
                    break;
                case "/cryptocoins":
                case var txt when txt == BotPhrases.BtnCryptoCoins:
                    await _cryptoHandler.HandleCryptoCoinsCommand(update);
                    break;
                default:
                    await _updateService.EchoTextMessageAsync(update, BotPhrases.Error, default);
                    break;
            }
        }

        private async Task RouteCallback(Update update)
        {
            var callbackData = update.CallbackQuery.Data;

            // Порядок проверок КРИТИЧЕН — более специфичные префиксы проверять раньше
            if (callbackData.StartsWith("crypto_"))
            {
                var currency = callbackData.EndsWith("usd") ? "USD" : "RUB";
                await _cryptoHandler.HandleCryptoCallback(update, currency);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            if (callbackData.StartsWith("news_p_"))
            {
                var idStr = callbackData.Substring(7);
                if (int.TryParse(idStr, out var beforeId))
                    await _newsHandler.HandleNewsPage(update, beforeId);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            if (callbackData.StartsWith("toggle_crypto_"))
            {
                var symbol = callbackData.Substring(14);
                await _cryptoHandler.HandleToggleCryptoSymbol(update, symbol);
                return;
            }

            if (callbackData.StartsWith("toggle_news_"))
            {
                var timeSlot = callbackData.Substring(12);
                await _newsHandler.HandleToggleNewsSlot(update, timeSlot);
                return;
            }

            if (callbackData.StartsWith("toggle_"))
            {
                var currencyCode = callbackData.Substring(7);
                await _currenciesHandler.HandleToggleCurrency(update, currencyCode);
                return;
            }

            if (callbackData.StartsWith("period_"))
            {
                var days = int.Parse(callbackData.Substring(7));
                await _statisticsHandler.HandlePeriodCallback(update, days);
                return;
            }

            switch (callbackData)
            {
                case "save_currencies":
                    await _currenciesHandler.HandleSaveCurrencies(update);
                    break;
                case "save_crypto_coins":
                    await _cryptoHandler.HandleSaveCryptoCoins(update);
                    break;
                case "sub_toggle_rates":
                    await _subscriptionHandler.HandleToggleRates(update);
                    break;
                case "sub_toggle_important":
                    await _subscriptionHandler.HandleToggleImportant(update);
                    break;
                case "sub_news_menu":
                    await _subscriptionHandler.HandleNewsMenu(update);
                    break;
                case "sub_news_toggle":
                    await _subscriptionHandler.HandleNewsToggle(update);
                    break;
                case "sub_back":
                    await _subscriptionHandler.HandleBack(update);
                    break;
                case "news_schedule":
                    await _newsHandler.HandleScheduleCommand(update);
                    break;
                case "save_news_schedule":
                    await _newsHandler.HandleSaveNewsSchedule(update);
                    break;
                case "news_latest":
                    await _newsHandler.HandleNewsLatest(update);
                    break;
                case "important_news_subscribe":
                case "important_news_unsubscribe":
                case "news_subscribe":
                case "news_unsubscribe":
                case "Подписаться":
                case "Отписаться":
                    await _subscriptionHandler.HandleLegacyCallbacks(update);
                    break;
            }
        }
    }
}
```

- [ ] **Step 2: Обновить Startup.cs — добавить DI-регистрацию**

В `src/bot/ExchangeRatesBot/Startup.cs`, в метод `ConfigureServices`, **перед** строкой `services.AddScoped<ICommandBot, CommandService>()` добавить:

```csharp
// BOT-0025: Singleton для in-memory состояния выбора
services.AddSingleton<IUserSelectionState, UserSelectionState>();

// BOT-0025: Доменные handler'ы
services.AddScoped<StartHandler>();
services.AddScoped<ValuteHandler>();
services.AddScoped<CurrenciesHandler>();
services.AddScoped<StatisticsHandler>();
services.AddScoped<SubscriptionHandler>();
services.AddScoped<NewsHandler>();
services.AddScoped<CryptoHandler>();
```

Также добавить using:
```csharp
using ExchangeRatesBot.App.Handlers;
```

- [ ] **Step 3: Собрать решение**

```bash
dotnet build src/ExchangeRates.Api.sln
```

Expected: 0 ошибок. Если есть ошибки — скорее всего неиспользуемые using'и в CommandService, убрать.

- [ ] **Step 4: Коммит**

```bash
git add src/bot/ExchangeRatesBot.App/Services/CommandService.cs src/bot/ExchangeRatesBot/Startup.cs
git commit -m "BOT-0025: CommandService → тонкий роутер + DI-регистрация handler'ов"
```

---

## Task 10: Docker-сборка и деплой

**Files:** Нет изменений в файлах

- [ ] **Step 1: Собрать и запустить в Docker**

```bash
docker-compose up -d --build
```

Expected: все 4 контейнера собраны и запущены без ошибок

- [ ] **Step 2: Проверить логи бота**

```bash
docker-compose logs -f exchangerates-bot
```

Expected: бот запустился без ошибок, DI-контейнер разрешил все зависимости

- [ ] **Step 3: Коммит (если были правки по результатам сборки)**

Если Docker-сборка выявила проблемы и они были исправлены:

```bash
git add -A
git commit -m "BOT-0025: Исправления после Docker-сборки"
```

---

## Task 11: Ручное тестирование в Telegram

**Files:** Нет изменений

Протестировать каждую команду и callback. Чеклист:

- [ ] **Step 1: Message commands**

| Команда | Ожидаемый результат |
|---------|---------------------|
| `/start` | Приветствие + reply-клавиатура (3 ряда) |
| `/help` | Текст помощи |
| Кнопка "Помощь" | Текст помощи |
| `/valuteoneday` | Курсы за 1 день |
| Кнопка "Курс сегодня" | Курсы за 1 день |
| `/valutesevendays` | Курсы за 7 дней |
| Кнопка "За 7 дней" | Курсы за 7 дней |
| `/statistics` | Inline-кнопки выбора периода |
| Кнопка "Статистика" | Inline-кнопки выбора периода |
| `/currencies` | Inline-клавиатура с 10 валютами |
| Кнопка "Валюты" | Inline-клавиатура с 10 валютами |
| `/subscribe` | Inline-меню подписок |
| Кнопка "Подписка" | Inline-меню подписок |
| `/news` | Лента последних 5 новостей |
| Кнопка "Новости" | Лента последних 5 новостей |
| `/crypto` | Курсы крипто в RUB |
| Кнопка "Крипто" | Курсы крипто в RUB |
| `/cryptocoins` | Inline-клавиатура с 10 монетами |
| Кнопка "Монеты" | Inline-клавиатура с 10 монетами |

- [ ] **Step 2: Callback queries**

| Callback | Ожидаемый результат |
|----------|---------------------|
| `crypto_rub` / `crypto_usd` | Переключение валюты крипто |
| `crypto_refresh_rub` | Обновление курсов |
| `toggle_USD` (любая валюта) | Toggle ✅/⬜ в клавиатуре |
| `save_currencies` | "Сохранено: USD,EUR,..." |
| `toggle_crypto_BTC` | Toggle ✅/⬜ монеты |
| `save_crypto_coins` | "Сохранено: BTC,ETH,..." |
| `period_3` / `period_7` / ... | Статистика за N дней |
| `sub_toggle_rates` | Toggle подписки на курсы |
| `sub_toggle_important` | Toggle подписки на важные новости |
| `sub_news_menu` | Подменю дайджеста |
| `sub_news_toggle` | Toggle подписки на дайджест |
| `sub_back` | Назад в меню подписок |
| `news_schedule` | Клавиатура расписания |
| `toggle_news_09` (любой слот) | Toggle ✅ слота |
| `save_news_schedule` | "Сохранено: 09:00,18:00" |
| `news_latest` | Последние 5 новостей |
| `news_p_{id}` | Пагинация (ещё 5 новостей) |

- [ ] **Step 3: Обновить статус ADR**

В файле `doc/architect/adr-bot-0025-command-service-decomposition.md` изменить:

```
**Статус**: Предложен
```
на:
```
**Статус**: Принят и реализован (2026-04-02)
```

- [ ] **Step 4: Обновить статус плана**

В файле `doc/architect/bot-0025-command-service-decomposition.md` изменить:

```
- [ ] Не реализовано
```
на:
```
- [x] Реализовано
```

- [ ] **Step 5: Финальный коммит**

```bash
git add doc/architect/adr-bot-0025-command-service-decomposition.md doc/architect/bot-0025-command-service-decomposition.md
git commit -m "BOT-0025: Декомпозиция CommandService — реализовано"
```
