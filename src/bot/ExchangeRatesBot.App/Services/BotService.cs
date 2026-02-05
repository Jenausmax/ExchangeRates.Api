using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace ExchangeRatesBot.App.Services
{
    public class BotService : IBotService
    {
        private readonly IOptions<BotConfig> _config;
        public TelegramBotClient Client { get; }

        public BotService(IOptions<BotConfig> config)
        {
            _config = config;
            Client = new TelegramBotClient(_config.Value.BotToken);
            Client.SetWebhookAsync(_config.Value.Webhook);
        }
    }
}
