using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Maintenance.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.DB.Models;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types.Enums;

namespace ExchangeRatesBot.Maintenance.Jobs
{
    public class JobsSendNewsDigest : BackgroundTaskAbstract<JobsSendNewsDigest>
    {
        private readonly IOptions<BotConfig> _config;
        private readonly ILogger _logger;

        public JobsSendNewsDigest(IServiceProvider services, IOptions<BotConfig> config, ILogger logger)
            : base(services, config, logger)
        {
            _config = config;
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken cancel, IServiceProvider scope)
        {
            DateTime timeNow;
            DateTime newsTime;

            var cur = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            DateTime.TryParseExact(cur, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out timeNow);
            DateTime.TryParse(_config.Value.NewsTime, out newsTime);

            if (timeNow != newsTime)
                return;

            _logger.Information("Starting news digest distribution...");

            var botService = scope.GetRequiredService<IBotService>();
            var newsClient = scope.GetRequiredService<INewsApiClient>();
            var repo = scope.GetRequiredService<IBaseRepositoryDb<UserDb>>();

            var usersCollectionDb = await repo.GetCollection(cancel);
            var subscribers = usersCollectionDb.Where(u => u.NewsSubscribe).ToArray();

            if (!subscribers.Any())
            {
                _logger.Information("No news subscribers found");
                return;
            }

            var digest = await newsClient.GetLatestDigestAsync(5, cancel);

            if (string.IsNullOrWhiteSpace(digest?.Message) || digest.TopicIds == null || !digest.TopicIds.Any())
            {
                _logger.Information("No new topics for news digest");
                return;
            }

            foreach (var user in subscribers)
            {
                try
                {
                    await botService.Client.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: digest.Message,
                        parseMode: ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to send news digest to user {ChatId}", user.ChatId);
                }
            }

            // Пометить темы как отправленные после успешной рассылки
            await newsClient.MarkSentAsync(digest.TopicIds, cancel);
            _logger.Information("News digest sent to {Count} subscribers, {TopicCount} topics marked as sent",
                subscribers.Length, digest.TopicIds.Count);
        }
    }
}
