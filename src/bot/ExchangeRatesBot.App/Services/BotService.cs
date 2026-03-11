using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace ExchangeRatesBot.App.Services
{
    public class BotService : IBotService
    {
        private readonly IOptions<BotConfig> _config;
        private readonly ILogger _logger;
        public TelegramBotClient Client { get; }

        public BotService(IOptions<BotConfig> config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            Client = new TelegramBotClient(_config.Value.BotToken);

            // Настройка режима работы бота (async initialization)
            Task.Run(async () =>
            {
                if (_config.Value.UsePolling)
                {
                    // Polling режим: удаляем webhook если он был установлен
                    await Client.DeleteWebhookAsync();
                    _logger.Information("Bot initialized in POLLING mode. Webhook removed.");
                }
                else
                {
                    // Webhook режим: устанавливаем webhook URL
                    if (string.IsNullOrWhiteSpace(_config.Value.Webhook))
                    {
                        _logger.Warning("Webhook mode enabled but Webhook URL is empty! Bot may not receive updates.");
                    }
                    else
                    {
                        await Client.SetWebhookAsync(_config.Value.Webhook);
                        _logger.Information($"Bot initialized in WEBHOOK mode. Webhook set to: {_config.Value.Webhook}");
                    }
                }
            }).GetAwaiter().GetResult();
        }
    }
}
