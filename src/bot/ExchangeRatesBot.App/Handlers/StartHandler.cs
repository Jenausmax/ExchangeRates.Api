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
                new[] { new KeyboardButton(BotPhrases.BtnRates), new KeyboardButton(BotPhrases.BtnNews), new KeyboardButton(BotPhrases.BtnStatistics) },
                new[] { new KeyboardButton(BotPhrases.BtnSettings), new KeyboardButton(BotPhrases.BtnHelp) }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
