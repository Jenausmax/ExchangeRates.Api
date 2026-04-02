using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
