using Telegram.Bot;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
    }
}
