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
        //private Update _update;

        public CommandService(IUpdateService updateService, 
            IProcessingService processingService,
            IMessageValute valuteService,
            IUserService userControl)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userControl = userControl;
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
                new[] { new KeyboardButton(BotPhrases.BtnValuteOneDay), new KeyboardButton(BotPhrases.BtnValuteSevenDays) },
                new[] { new KeyboardButton(BotPhrases.BtnSubscribe),    new KeyboardButton(BotPhrases.BtnHelp) }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
