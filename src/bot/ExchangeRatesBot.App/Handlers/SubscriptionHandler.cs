using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
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
