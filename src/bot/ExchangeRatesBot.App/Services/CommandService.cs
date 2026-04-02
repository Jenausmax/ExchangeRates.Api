using System;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Handlers;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
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
