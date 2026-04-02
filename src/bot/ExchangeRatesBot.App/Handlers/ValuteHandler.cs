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
