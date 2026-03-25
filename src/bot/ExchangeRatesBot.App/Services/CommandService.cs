using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        private readonly IKriptoApiClient _kriptoClient;

        // Временное состояние выбора валют для каждого пользователя (chatId -> HashSet<currencies>)
        private static readonly ConcurrentDictionary<long, HashSet<string>> _pendingSelections = new();
        // Временное состояние выбора расписания новостей (chatId -> HashSet<time slots>)
        private static readonly ConcurrentDictionary<long, HashSet<string>> _pendingNewsSchedule = new();
        // Временное состояние выбора криптовалют (chatId -> HashSet<symbols>)
        private static readonly ConcurrentDictionary<long, HashSet<string>> _pendingCryptoSelections = new();

        public CommandService(IUpdateService updateService,
            IProcessingService processingService,
            IMessageValute valuteService,
            IUserService userControl,
            IBotService botService,
            INewsApiClient newsClient,
            IKriptoApiClient kriptoClient)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userControl = userControl;
            _botService = botService;
            _newsClient = newsClient;
            _kriptoClient = kriptoClient;
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

            // Криптовалюты: crypto_rub, crypto_usd, crypto_refresh_rub, crypto_refresh_usd
            if (callbackData.StartsWith("crypto_"))
            {
                var currency = callbackData.EndsWith("usd") ? "USD" : "RUB";
                await HandleCryptoCallback(update, currency);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            // Пагинация новостей: news_p_{id}
            if (callbackData.StartsWith("news_p_"))
            {
                var idStr = callbackData.Substring(7); // "news_p_42" -> "42"
                if (int.TryParse(idStr, out var beforeId))
                {
                    await HandleNewsPage(update, beforeId);
                }
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            // Toggle криптомонеты — ВАЖНО: проверять ДО toggle_ (фиат)
            if (callbackData.StartsWith("toggle_crypto_"))
            {
                var symbol = callbackData.Substring(14); // "toggle_crypto_BTC" -> "BTC"
                await HandleToggleCryptoSymbol(update, chatId, symbol);
                return;
            }

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

                case "save_crypto_coins":
                    await HandleSaveCryptoCoins(update, chatId);
                    break;

                // --- Меню подписок: toggle курсов валют ---
                case "sub_toggle_rates":
                    {
                        var currentRates = _userControl.CurrentUser?.Subscribe == true;
                        await _userControl.SubscribeUpdate(chatId, !currentRates, CancellationToken.None);
                        await _userControl.SetUser(chatId); // обновить CurrentUser
                        await _botService.Client.EditMessageReplyMarkupAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            messageId: update.CallbackQuery.Message.MessageId,
                            replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
                        await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                            currentRates ? "Подписка на курсы отменена" : "Подписка на курсы оформлена");
                        break;
                    }

                // --- Меню подписок: toggle важных новостей ---
                case "sub_toggle_important":
                    {
                        var currentImportant = _userControl.CurrentUser?.ImportantNewsSubscribe == true;
                        await _userControl.ImportantNewsSubscribeUpdate(chatId, !currentImportant, CancellationToken.None);
                        await _userControl.SetUser(chatId); // обновить CurrentUser
                        await _botService.Client.EditMessageReplyMarkupAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            messageId: update.CallbackQuery.Message.MessageId,
                            replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
                        await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                            currentImportant ? "Подписка на важные новости отменена" : "Подписка на важные новости оформлена");
                        break;
                    }

                // --- Меню подписок: открыть подменю новостного дайджеста ---
                case "sub_news_menu":
                    await _botService.Client.EditMessageTextAsync(
                        chatId: update.CallbackQuery.Message.Chat.Id,
                        messageId: update.CallbackQuery.Message.MessageId,
                        text: BotPhrases.NewsDigestMenuHeader,
                        replyMarkup: new InlineKeyboardMarkup(NewsSubscriptionMenu()));
                    await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                    break;

                // --- Подменю дайджеста: toggle подписки ---
                case "sub_news_toggle":
                    {
                        var currentNews = _userControl.CurrentUser?.NewsSubscribe == true;
                        if (currentNews)
                        {
                            await _userControl.UpdateNewsTimes(chatId, null, CancellationToken.None);
                            await _userControl.NewsSubscribeUpdate(chatId, false, CancellationToken.None);
                        }
                        else
                        {
                            await _userControl.UpdateNewsTimes(chatId, "09:00", CancellationToken.None);
                            await _userControl.NewsSubscribeUpdate(chatId, true, CancellationToken.None);
                        }
                        await _userControl.SetUser(chatId); // обновить CurrentUser
                        await _botService.Client.EditMessageReplyMarkupAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            messageId: update.CallbackQuery.Message.MessageId,
                            replyMarkup: new InlineKeyboardMarkup(NewsSubscriptionMenu()));
                        await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                            currentNews ? "Подписка на новости отменена" : "Подписка на новости оформлена (09:00)");
                        break;
                    }

                // --- Подменю дайджеста: назад в главное меню подписок ---
                case "sub_back":
                    await _botService.Client.EditMessageTextAsync(
                        chatId: update.CallbackQuery.Message.Chat.Id,
                        messageId: update.CallbackQuery.Message.MessageId,
                        text: BotPhrases.SubscriptionMenuHeader,
                        replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu()));
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
                    break;

                // old callbacks kept for backward compatibility (no-op, redirect to new menu)
                case "important_news_subscribe":
                case "important_news_unsubscribe":
                case "news_subscribe":
                case "news_unsubscribe":
                case "Подписаться":
                case "Отписаться":
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.SubscriptionMenuHeader,
                        new InlineKeyboardMarkup(SubscriptionMenu()));
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
                        BotPhrases.SubscriptionMenuHeader,
                        new InlineKeyboardMarkup(SubscriptionMenu()));
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

                // --- Команда /cryptocoins и кнопка "Монеты" ---
                case "/cryptocoins":
                case var txt when txt == BotPhrases.BtnCryptoCoins:
                    {
                        var currentCoins = _userControl.GetUserCryptoCoins(_userControl.CurrentUser.ChatId);
                        _pendingCryptoSelections[_userControl.CurrentUser.ChatId] = currentCoins != null
                            ? new HashSet<string>(currentCoins)
                            : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
                        await _updateService.EchoTextMessageAsync(
                            update,
                            BotPhrases.CryptoCoinsHeader,
                            new InlineKeyboardMarkup(CryptoCoinsKeyboard(_userControl.CurrentUser.ChatId)));
                        break;
                    }

                // --- Команда /crypto и кнопка "Крипто" ---
                case "/crypto":
                case var txt when txt == BotPhrases.BtnCrypto:
                    await HandleCryptoCommand(update, "RUB");
                    break;

                // --- Команда /news и кнопка "Новости" — сразу лента новостей ---
                case "/news":
                case var txt when txt == BotPhrases.BtnNews:
                    {
                        var newsDigest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
                        if (string.IsNullOrWhiteSpace(newsDigest?.Message))
                        {
                            await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
                        }
                        else
                        {
                            var newsReplyMarkup = newsDigest.HasMore && newsDigest.TopicIds?.Count > 0
                                ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                {
                                    new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{newsDigest.TopicIds.Last()}")
                                    }
                                })
                                : null;
                            await _updateService.EchoTextMessageAsync(update, newsDigest.Message, newsReplyMarkup);
                        }
                        break;
                    }

                default:
                    await _updateService.EchoTextMessageAsync(
                        update,
                        BotPhrases.Error,
                        default);
                    break;
            }
        }

        /// <summary>
        /// Главное меню подписок с актуальным статусом.
        /// </summary>
        private List<List<InlineKeyboardButton>> SubscriptionMenu()
        {
            var user = _userControl.CurrentUser;
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

        /// <summary>
        /// Подменю новостного дайджеста с toggle и расписанием.
        /// </summary>
        private List<List<InlineKeyboardButton>> NewsSubscriptionMenu()
        {
            var user = _userControl.CurrentUser;
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
                new[] { new KeyboardButton(BotPhrases.BtnNews), new KeyboardButton(BotPhrases.BtnCrypto), new KeyboardButton(BotPhrases.BtnCryptoCoins) }
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


        private async Task HandleNewsPage(Update update, int beforeId)
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

        // --- Криптовалюты ---

        /// <summary>
        /// Обработка команды /crypto и кнопки "Крипто" (новое сообщение)
        /// </summary>
        private async Task HandleCryptoCommand(Update update, string currency)
        {
            var coins = _userControl.GetUserCryptoCoins(_userControl.CurrentUser.ChatId);
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

        /// <summary>
        /// Обработка inline-callback'ов крипто (редактирование сообщения)
        /// </summary>
        private async Task HandleCryptoCallback(Update update, string currency)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;
            var coins = _userControl.GetUserCryptoCoins(update.CallbackQuery.From.Id);
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

        /// <summary>
        /// Форматирование курсов криптовалют для Telegram
        /// </summary>
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

        /// <summary>
        /// Форматирование цены с разделителями тысяч
        /// </summary>
        private static string FormatCryptoPrice(decimal price)
        {
            if (price >= 1000)
                return price.ToString("N0", CultureInfo.InvariantCulture);
            if (price >= 1)
                return price.ToString("N2", CultureInfo.InvariantCulture);
            return price.ToString("N4", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Экранирование специальных символов Markdown для Telegram
        /// </summary>
        private static string EscapeMarkdown(string text)
            => text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("`", "\\`");

        /// <summary>
        /// Inline-клавиатура для крипто: переключение валюты + обновление
        /// </summary>
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

        // --- Персонализация криптовалют ---

        /// <summary>
        /// Toggle криптомонеты в pending-выборе
        /// </summary>
        private async Task HandleToggleCryptoSymbol(Update update, long chatId, string symbol)
        {
            if (!_pendingCryptoSelections.ContainsKey(chatId))
            {
                var currentCoins = _userControl.GetUserCryptoCoins(chatId);
                _pendingCryptoSelections[chatId] = currentCoins != null
                    ? new HashSet<string>(currentCoins)
                    : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
            }

            var selection = _pendingCryptoSelections[chatId];
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

        /// <summary>
        /// Сохранить выбор криптомонет в БД
        /// </summary>
        private async Task HandleSaveCryptoCoins(Update update, long chatId)
        {
            if (!_pendingCryptoSelections.ContainsKey(chatId) || _pendingCryptoSelections[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoCoinsEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _pendingCryptoSelections[chatId];
            var coinsString = string.Join(",", selected);
            await _userControl.UpdateCryptoCoins(chatId, coinsString, CancellationToken.None);

            _pendingCryptoSelections.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CryptoCoinsSaved + coinsString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Inline-клавиатура для выбора криптовалют
        /// </summary>
        private List<List<InlineKeyboardButton>> CryptoCoinsKeyboard(long chatId)
        {
            var selected = _pendingCryptoSelections.ContainsKey(chatId)
                ? _pendingCryptoSelections[chatId]
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
    }
}
