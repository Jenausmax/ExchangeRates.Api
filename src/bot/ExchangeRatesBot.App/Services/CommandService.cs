using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ExchangeRatesBot.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using Telegram.Bot;
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
        private readonly IBotService _botService;
        private readonly INewsApiClient _newsClient;

        // Временное состояние выбора валют для каждого пользователя (chatId -> HashSet<currencies>)
        private static readonly ConcurrentDictionary<long, HashSet<string>> _pendingSelections = new();
        // Временное состояние выбора расписания новостей (chatId -> HashSet<time slots>)
        private static readonly ConcurrentDictionary<long, HashSet<string>> _pendingNewsSchedule = new();
        //private Update _update;

        public CommandService(IUpdateService updateService,
            IProcessingService processingService,
            IMessageValute valuteService,
            IUserService userControl,
            IBotService botService,
            INewsApiClient newsClient)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userControl = userControl;
            _botService = botService;
            _newsClient = newsClient;
        }

        //public async Task SetUpdateBot(Update update)
        //{
        //    _update = update;
        //}

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

            // Установить текущего пользователя для callback
            var chatId = update.CallbackQuery.From.Id;
            await _userControl.SetUser(chatId);

            // Toggle расписания новостей
            if (callbackData.StartsWith("toggle_news_"))
            {
                var timeSlot = callbackData.Substring(12); // "toggle_news_09" -> "09"
                await HandleToggleNewsSlot(update, chatId, timeSlot);
                return;
            }

            // Toggle валюты
            if (callbackData.StartsWith("toggle_"))
            {
                var currencyCode = callbackData.Substring(7); // "toggle_USD" -> "USD"
                await HandleToggleCurrency(update, chatId, currencyCode);
                return;
            }

            // Выбор периода статистики
            if (callbackData.StartsWith("period_"))
            {
                var days = int.Parse(callbackData.Substring(7)); // "period_3" -> 3
                var currencies = _userControl.GetUserCurrencies(_userControl.CurrentUser.ChatId);

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
                return;
            }

            switch (callbackData)
            {
                case "save_currencies":
                    await HandleSaveCurrencies(update, chatId);
                    break;

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

                case "news_subscribe":
                    {
                        var currentNewsTimes = _userControl.CurrentUser?.NewsTimes;
                        if (!string.IsNullOrEmpty(currentNewsTimes))
                        {
                            await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsAlreadySubscribed, default);
                        }
                        else
                        {
                            await _userControl.UpdateNewsTimes(chatId, "09:00", CancellationToken.None);
                            await _userControl.NewsSubscribeUpdate(chatId, true, CancellationToken.None);
                            await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsSubscribeTrueSchedule, default);
                        }
                        await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                        break;
                    }

                case "news_unsubscribe":
                    await _userControl.UpdateNewsTimes(chatId, null, CancellationToken.None);
                    await _userControl.NewsSubscribeUpdate(chatId, false, CancellationToken.None);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.NewsSubscribeFalse,
                        default);
                    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                    break;

                case "news_schedule":
                    {
                        var currentTimes = _userControl.GetUserNewsTimes(chatId);
                        _pendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
                        await _updateService.EchoTextMessageAsync(
                            update,
                            BotPhrases.NewsScheduleHeader,
                            new InlineKeyboardMarkup(NewsScheduleKeyboard(chatId)));
                        await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                        break;
                    }

                case "save_news_schedule":
                    await HandleSaveNewsSchedule(update, chatId);
                    break;

                case "news_latest":
                    var digest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
                    if (string.IsNullOrWhiteSpace(digest?.Message))
                    {
                        await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
                    }
                    else
                    {
                        await _updateService.EchoTextMessageAsync(update, digest.Message, default);
                    }
                    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                    break;
            }
        }

        private async Task MessageCommand(Update update)
        {
            var message = update.Message.Text;
            if (string.IsNullOrEmpty(message))
            {
                await _updateService.EchoTextMessageAsync(
                    update,
                    BotPhrases.Error,
                    default);
                return;
            }
            switch (message)
            {
                // --- Команда /start: приветствие + ReplyKeyboard ---
                case "/start":
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.StartMenu + $"\n\r /subscribe - подписка \n\r /currencies - выбор валют \n\r /valuteoneday - курс на сегодня \n\r /valutesevendays - изменения курса за последние 7 дней \n\r\n\r*Используйте кнопки меню внизу чата!*",
                        GetMainKeyboard());
                    break;

                // --- Команда /currencies: выбор валют для отслеживания ---
                case "/currencies":
                case var txt when txt == BotPhrases.BtnCurrencies:
                    var currentCurrencies = _userControl.GetUserCurrencies(_userControl.CurrentUser.ChatId);
                    _pendingSelections[_userControl.CurrentUser.ChatId] = new HashSet<string>(currentCurrencies);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.CurrenciesHeader,
                        new InlineKeyboardMarkup(CurrenciesKeyboard(_userControl.CurrentUser.ChatId)));
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
                    var valutes7 = _userControl.GetUserCurrencies(_userControl.CurrentUser.ChatId);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        await _valuteService.GetValuteMessage(8, valutes7, CancellationToken.None),
                        default);
                    break;

                // --- Команда /valuteoneday и кнопка "Курс сегодня" ---
                case "/valuteoneday":
                case var txt when txt == BotPhrases.BtnValuteOneDay:
                    var valutes1 = _userControl.GetUserCurrencies(_userControl.CurrentUser.ChatId);
                    await _updateService.EchoTextMessageAsync(
                        update,
                        await _valuteService.GetValuteMessage(1, valutes1, CancellationToken.None),
                        default);
                    break;

                // --- Команда /statistics и кнопка "Статистика" ---
                case "/statistics":
                case var txt when txt == BotPhrases.BtnStatistics:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        "📊 Выберите период для статистики:",
                        new InlineKeyboardMarkup(PeriodSelectionKeyboard()));
                    break;

                // --- Команда /news и кнопка "Новости" ---
                case "/news":
                case var txt when txt == BotPhrases.BtnNews:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.NewsHeader,
                        new InlineKeyboardMarkup(NewsMenu()));
                    break;

                default:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.Error,
                        default);
                    break;
            }
        }

        private List<InlineKeyboardButton> Menu()
        {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(InlineKeyboardButton.WithCallbackData("Подписаться"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("Отписаться"));
            return buttons;
        }

        /// <summary>
        /// Создает постоянную клавиатуру бота (ReplyKeyboardMarkup).
        /// Отображается внизу чата и остается до явного удаления.
        /// </summary>
        private static ReplyKeyboardMarkup GetMainKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(BotPhrases.BtnValuteOneDay), new KeyboardButton(BotPhrases.BtnValuteSevenDays), new KeyboardButton(BotPhrases.BtnStatistics) },
                new[] { new KeyboardButton(BotPhrases.BtnCurrencies),     new KeyboardButton(BotPhrases.BtnSubscribe),     new KeyboardButton(BotPhrases.BtnHelp) },
                new[] { new KeyboardButton(BotPhrases.BtnNews) }
            })
            {
                ResizeKeyboard = true
            };
        }

        /// <summary>
        /// Обработка нажатия на кнопку валюты (toggle)
        /// </summary>
        private async Task HandleToggleCurrency(Update update, long chatId, string currencyCode)
        {
            if (!_pendingSelections.ContainsKey(chatId))
            {
                // Сессия выбора истекла (перезапуск бота) -- инициализировать из БД
                var currentCurrencies = _userControl.GetUserCurrencies(chatId);
                _pendingSelections[chatId] = new HashSet<string>(currentCurrencies);
            }

            var selection = _pendingSelections[chatId];
            if (selection.Contains(currencyCode))
                selection.Remove(currencyCode);
            else
                selection.Add(currencyCode);

            // Обновить клавиатуру без отправки нового сообщения
            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(CurrenciesKeyboard(chatId)));

            // Ответить на callback, чтобы убрать "часики" на кнопке
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Обработка нажатия на кнопку "Сохранить"
        /// </summary>
        private async Task HandleSaveCurrencies(Update update, long chatId)
        {
            if (!_pendingSelections.ContainsKey(chatId) || _pendingSelections[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CurrenciesEmpty, default);
                return;
            }

            var selected = _pendingSelections[chatId];
            var currenciesString = string.Join(",", selected);
            await _userControl.UpdateCurrencies(chatId, currenciesString, CancellationToken.None);

            // Очистить временное состояние
            _pendingSelections.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CurrenciesSaved + currenciesString,
                default);
        }

        /// <summary>
        /// Генерация inline-клавиатуры для выбора валют
        /// </summary>
        private List<List<InlineKeyboardButton>> CurrenciesKeyboard(long chatId)
        {
            var selected = _pendingSelections.ContainsKey(chatId)
                ? _pendingSelections[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var currency in BotPhrases.AvailableCurrencies)
            {
                var isSelected = selected.Contains(currency);
                var label = isSelected ? $"✅ {currency}" : $"⬜ {currency}";
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_{currency}"));

                if (currentRow.Count == 3)  // по 3 кнопки в ряд
                {
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);

            // Кнопка "Сохранить" -- отдельный ряд
            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", "save_currencies")
            });

            return rows;
        }

        /// <summary>
        /// Генерация inline-клавиатуры для выбора периода статистики.
        /// </summary>
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

        /// <summary>
        /// Генерация inline-клавиатуры меню новостного дайджеста.
        /// </summary>
        private static List<List<InlineKeyboardButton>> NewsMenu()
        {
            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Последние новости", "news_latest")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Настроить расписание", "news_schedule")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Подписаться", "news_subscribe"),
                    InlineKeyboardButton.WithCallbackData("Отписаться", "news_unsubscribe")
                }
            };
        }

        private async Task HandleToggleNewsSlot(Update update, long chatId, string timeSlotKey)
        {
            if (!_pendingNewsSchedule.ContainsKey(chatId))
            {
                var currentTimes = _userControl.GetUserNewsTimes(chatId);
                _pendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
            }

            // timeSlotKey = "09" -> fullSlot = "09:00"
            var fullSlot = timeSlotKey + ":00";
            var selection = _pendingNewsSchedule[chatId];
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

        private async Task HandleSaveNewsSchedule(Update update, long chatId)
        {
            if (!_pendingNewsSchedule.ContainsKey(chatId) || _pendingNewsSchedule[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsScheduleEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _pendingNewsSchedule[chatId];
            var sortedSlots = selected.OrderBy(s => s).ToArray();
            var newsTimesString = string.Join(",", sortedSlots);
            await _userControl.UpdateNewsTimes(chatId, newsTimesString, CancellationToken.None);
            await _userControl.NewsSubscribeUpdate(chatId, true, CancellationToken.None);

            _pendingNewsSchedule.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.NewsScheduleSaved + newsTimesString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> NewsScheduleKeyboard(long chatId)
        {
            var selected = _pendingNewsSchedule.ContainsKey(chatId)
                ? _pendingNewsSchedule[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var slot in BotPhrases.AvailableNewsSlots)
            {
                var isSelected = selected.Contains(slot);
                var label = isSelected ? $"✅ {slot}" : slot;
                var slotKey = slot.Substring(0, 2); // "09:00" -> "09"
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
