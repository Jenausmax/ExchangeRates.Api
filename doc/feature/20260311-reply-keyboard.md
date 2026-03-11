- [ ] Не реализовано

# Reply Keyboard -- постоянная клавиатура бота

## Цель

Добавить постоянную клавиатуру (ReplyKeyboardMarkup) в нижнюю часть чата Telegram, чтобы пользователь мог взаимодействовать с ботом без ручного ввода текстовых команд. Клавиатура появляется при `/start` и остается на экране постоянно.

## Текущее поведение

- Бот принимает только текстовые команды: `/start`, `/subscribe`, `/valuteoneday`, `/valutesevendays`
- При `/start` отправляется текстовое сообщение со списком команд (без клавиатуры)
- Для подписки используется InlineKeyboardMarkup (кнопки "Подписаться" / "Отписаться" под сообщением)
- Метод `EchoTextMessageAsync` принимает только `InlineKeyboardMarkup`
- Команда `/help` отсутствует
- При нераспознанном вводе отправляется `BotPhrases.Error`

## Желаемое поведение

- При `/start` бот отправляет приветственное сообщение с постоянной ReplyKeyboard из 4 кнопок
- Нажатие на кнопку клавиатуры отправляет текст кнопки как обычное сообщение
- CommandService обрабатывает текст кнопки наравне со слеш-командами
- Все существующие слеш-команды продолжают работать (обратная совместимость)

## Схема клавиатуры

```
+---------------------+---------------------+
|  Курс сегодня       |  За 7 дней          |
+---------------------+---------------------+
|  Подписка           |  Помощь             |
+---------------------+---------------------+
```

Параметр `ResizeKeyboard = true` -- клавиатура подстраивается под размер экрана.

## Маппинг кнопок на команды

| Текст кнопки       | Эквивалентная команда | Действие                                    |
|---------------------|-----------------------|---------------------------------------------|
| Курс сегодня        | /valuteoneday         | Курсы валют за сегодня (день = 1)           |
| За 7 дней           | /valutesevendays      | Изменения курса за 7 дней (день = 8)        |
| Подписка            | /subscribe            | Inline-меню "Подписаться" / "Отписаться"    |
| Помощь              | /help (новая)         | Справочное сообщение со списком команд       |

## Список изменений по файлам

### 1. BotPhrases.cs

**Файл**: `src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs`

Добавить константы текстов кнопок и текст помощи.

```csharp
namespace ExchangeRatesBot.App.Phrases
{
    public static class BotPhrases
    {
        // --- Существующие фразы (без изменений) ---
        public static string StartMenu { get; } = "Доброго времени суток! *Подписка* - получать курсы валют ЦБ РФ на USD, EUR, CNY, GBP, JPY за последние 7 дней.";
        public static string SubscribeTrue { get; } = "*Подписка оформлена!* Вы будете получать сообщения 2 раза в сутки. Спасибо!";
        public static string SubscribeFalse { get; } = "*Подписка отменена!* Мне очень жаль что вы отписались :((.";
        public static string Error { get; } = "Не правильный запрос. Попробуйте воспользоваться меню снизу.";
        public static string[] Valutes { get; } = new string[] { "USD", "EUR", "GBP", "JPY", "CNY" };

        // --- Новые константы ---

        /// <summary>
        /// Тексты кнопок ReplyKeyboard. Используются и для создания клавиатуры,
        /// и для маппинга входящего текста на команды в CommandService.
        /// </summary>
        public static string BtnValuteOneDay { get; } = "Курс сегодня";
        public static string BtnValuteSevenDays { get; } = "За 7 дней";
        public static string BtnSubscribe { get; } = "Подписка";
        public static string BtnHelp { get; } = "Помощь";

        /// <summary>
        /// Текст ответа на команду /help.
        /// </summary>
        public static string HelpMessage { get; } =
            "*Доступные команды:*\n\r" +
            "Курс сегодня -- курсы валют ЦБ РФ на сегодня\n\r" +
            "За 7 дней -- изменения курсов за последние 7 дней\n\r" +
            "Подписка -- подписаться/отписаться от рассылки курсов\n\r" +
            "Помощь -- это сообщение\n\r\n\r" +
            "_Также доступны команды:_ /valuteoneday, /valutesevendays, /subscribe, /help";
    }
}
```

**Обоснование**: константы кнопок вынесены в BotPhrases, чтобы маппинг текста на команды в CommandService ссылался на те же строки, что и сама клавиатура. Это исключает рассинхронизацию текстов.

---

### 2. IUpdateService.cs

**Файл**: `src/bot/ExchangeRatesBot.Domain/Interfaces/IUpdateService.cs`

Текущая сигнатура принимает только `InlineKeyboardMarkup`. Нужно добавить перегрузку (или заменить тип параметра на базовый `IReplyMarkup`), чтобы поддержать `ReplyKeyboardMarkup`.

**Вариант: замена типа параметра на IReplyMarkup** (рекомендуется -- минимум изменений, максимум гибкости):

```csharp
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Метод отправки сообщения в чат от бота.
        /// </summary>
        /// <param name="update">Пришедший апдейт</param>
        /// <param name="message">Новое сообщение.</param>
        /// <param name="replyMarkup">Клавиатура для взаимодействия (Inline или Reply).</param>
        /// <returns></returns>
        Task EchoTextMessageAsync(Update update, string message, IReplyMarkup replyMarkup = default);
    }
}
```

**Важно**: `IReplyMarkup` -- базовый интерфейс в Telegram.Bot v16.0.2, от которого наследуются и `InlineKeyboardMarkup`, и `ReplyKeyboardMarkup`. Замена типа параметра с `InlineKeyboardMarkup` на `IReplyMarkup` **не ломает** существующие вызовы, т.к. `InlineKeyboardMarkup` реализует `IReplyMarkup`.

---

### 3. UpdateService.cs

**Файл**: `src/bot/ExchangeRatesBot.App/Services/UpdateService.cs`

Изменить тип параметра `keyboard` на `IReplyMarkup` в соответствии с интерфейсом.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly IBotService _botService;

        public UpdateService(IBotService botService)
        {
            _botService = botService;
        }

        public async Task EchoTextMessageAsync(Update update, string message, IReplyMarkup replyMarkup = default)
        {
            if (update == null) return;

            if (update.Type == UpdateType.Message)
            {
                if (update.Message != null)
                {
                    var newMessage = update.Message;
                    newMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(newMessage.Chat.Id,
                        newMessage.Text,
                        parseMode: ParseMode.Markdown,
                        disableWebPagePreview: false,
                        disableNotification: false,
                        replyToMessageId: 0,
                        replyMarkup: replyMarkup);
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery.Message != null)
                {
                    var newMessageCallbackQueryMessage = update.CallbackQuery.Message;
                    newMessageCallbackQueryMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(newMessageCallbackQueryMessage.Chat.Id,
                        newMessageCallbackQueryMessage.Text,
                        parseMode: ParseMode.Markdown,
                        disableWebPagePreview: false,
                        disableNotification: false,
                        replyToMessageId: 0,
                        replyMarkup: replyMarkup);
                }
            }

            if (update.Type == UpdateType.ChannelPost)
            {
                return;
            }
        }
    }
}
```

**Изменения**:
- Параметр `InlineKeyboardMarkup keyboard = default` заменен на `IReplyMarkup replyMarkup = default`
- Переменная переименована с `keyboard` на `replyMarkup` для ясности
- `SendTextMessageAsync` в Telegram.Bot v16.0.2 уже принимает `IReplyMarkup` в параметре `replyMarkup`, поэтому замена безопасна

---

### 4. CommandService.cs

**Файл**: `src/bot/ExchangeRatesBot.App/Services/CommandService.cs`

Основные изменения:

1. Добавить приватный метод `GetMainKeyboard()` для создания ReplyKeyboardMarkup
2. Расширить `switch` в `MessageCommand` для обработки текстов кнопок
3. Добавить обработку команд `/help` и текста кнопки "Помощь"
4. При `/start` отправлять сообщение с ReplyKeyboard

```csharp
using System.Collections.Generic;
using ExchangeRatesBot.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Services
{
    public class CommandService : ICommandBot
    {
        private readonly IUpdateService _updateService;
        private readonly IMessageValute _valuteService;
        private readonly IUserService _userControl;

        public CommandService(IUpdateService updateService,
            IProcessingService processingService,
            IMessageValute valuteService,
            IUserService userControl)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userControl = userControl;
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

                    await MessageCommand(update);
                    break;

                case UpdateType.CallbackQuery:
                    await CallbackMessageCommand(update);
                    break;

                default:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.Error,
                        default);
                    break;
            }
        }

        private async Task CallbackMessageCommand(Update update)
        {
            var callbackData = update.CallbackQuery.Data;
            switch (callbackData)
            {
                case "Подписаться":
                    await _userControl.SubscribeUpdate(_userControl.CurrentUser.ChatId, true, CancellationToken.None);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.SubscribeTrue,
                        default);
                    break;

                case "Отписаться":
                    await _userControl.SubscribeUpdate(_userControl.CurrentUser.ChatId, false, CancellationToken.None);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.SubscribeFalse,
                        default);
                    break;
            }
        }

        private async Task MessageCommand(Update update)
        {
            var message = update.Message.Text;
            switch (message)
            {
                // --- Команда /start: приветствие + ReplyKeyboard ---
                case "/start":
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.StartMenu,
                        GetMainKeyboard());
                    break;

                // --- Команда /help и кнопка "Помощь" ---
                case "/help":
                case var txt when txt == BotPhrases.BtnHelp:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.HelpMessage);
                    break;

                // --- Команда /subscribe и кнопка "Подписка" ---
                case "/subscribe":
                case var txt when txt == BotPhrases.BtnSubscribe:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.StartMenu,
                        new InlineKeyboardMarkup(Menu()));
                    break;

                // --- Команда /valutesevendays и кнопка "За 7 дней" ---
                case "/valutesevendays":
                case var txt when txt == BotPhrases.BtnValuteSevenDays:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        await _valuteService.GetValuteMessage(8, BotPhrases.Valutes, CancellationToken.None),
                        default);
                    break;

                // --- Команда /valuteoneday и кнопка "Курс сегодня" ---
                case "/valuteoneday":
                case var txt when txt == BotPhrases.BtnValuteOneDay:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        await _valuteService.GetValuteMessage(1, BotPhrases.Valutes, CancellationToken.None),
                        default);
                    break;

                default:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.Error,
                        default);
                    break;
            }
        }

        /// <summary>
        /// Создает постоянную клавиатуру бота (ReplyKeyboardMarkup).
        /// Отображается внизу чата и остается до явного удаления.
        /// </summary>
        private static ReplyKeyboardMarkup GetMainKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(BotPhrases.BtnValuteOneDay), new KeyboardButton(BotPhrases.BtnValuteSevenDays) },
                new[] { new KeyboardButton(BotPhrases.BtnSubscribe),    new KeyboardButton(BotPhrases.BtnHelp) }
            })
            {
                ResizeKeyboard = true
            };
        }

        private List<InlineKeyboardButton> Menu()
        {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(InlineKeyboardButton.WithCallbackData("Подписаться"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Отписаться"));
            return buttons;
        }
    }
}
```

**Ключевые решения**:

1. **Pattern matching в switch**: конструкция `case var txt when txt == BotPhrases.BtnHelp` позволяет сопоставлять текст кнопки с константой из BotPhrases. Слеш-команды идут первыми (literal case), текст кнопок -- вторыми. Это корректно работает в C# 7+ (.NET 5).

2. **ReplyKeyboard отправляется только при /start**: клавиатура Telegram запоминается на клиенте и остается видимой для пользователя до тех пор, пока не будет явно заменена или удалена. Повторная отправка при каждом сообщении не нужна.

3. **Текст /start упрощен**: убрана строка со списком команд, т.к. теперь есть клавиатура и команда /help.

---

## Файлы без изменений

Следующие файлы **не требуют изменений**:

- **JobsSendMessageUsers.cs** -- рассылка подписчикам не использует `EchoTextMessageAsync`, а напрямую вызывает `SendTextMessageAsync` с `ParseMode.Markdown` без клавиатуры. Это корректно: фоновая рассылка не должна отправлять ReplyKeyboard.
- **BotService.cs** -- без изменений, управление клиентом остается прежним.
- **PollingBackgroundService.cs** -- без изменений, передает Update в CommandService как и раньше.
- **UpdateController.cs** -- без изменений, передает Update в CommandService.

---

## Порядок реализации

1. Обновить `BotPhrases.cs` -- добавить константы кнопок и текст помощи
2. Обновить `IUpdateService.cs` -- заменить `InlineKeyboardMarkup` на `IReplyMarkup`
3. Обновить `UpdateService.cs` -- заменить тип параметра на `IReplyMarkup`
4. Обновить `CommandService.cs` -- добавить обработку текста кнопок, метод `GetMainKeyboard()`, команду `/help`
5. Собрать решение: `dotnet build src/ExchangeRates.Api.sln`
6. Протестировать в Docker: `docker-compose up -d --build`

---

## Критерии приемки

1. **ReplyKeyboard при /start**: после отправки `/start` в нижней части чата появляется клавиатура с 4 кнопками в 2 ряда
2. **Кнопка "Курс сегодня"**: при нажатии бот отвечает курсами валют за 1 день (аналог /valuteoneday)
3. **Кнопка "За 7 дней"**: при нажатии бот отвечает курсами за 7 дней (аналог /valutesevendays)
4. **Кнопка "Подписка"**: при нажатии бот показывает inline-кнопки "Подписаться" / "Отписаться"
5. **Кнопка "Помощь"**: при нажатии бот отвечает справочным сообщением с описанием всех команд
6. **Обратная совместимость**: слеш-команды `/start`, `/subscribe`, `/valuteoneday`, `/valutesevendays` продолжают работать
7. **Новая команда /help**: работает и как слеш-команда, и как кнопка
8. **Клавиатура не пропадает**: после нажатия на любую кнопку клавиатура остается на экране
9. **Фоновая рассылка**: JobsSendMessageUsers продолжает отправлять сообщения подписчикам без клавиатуры (без регрессии)
10. **Сборка проходит**: `dotnet build src/ExchangeRates.Api.sln` без ошибок и предупреждений
