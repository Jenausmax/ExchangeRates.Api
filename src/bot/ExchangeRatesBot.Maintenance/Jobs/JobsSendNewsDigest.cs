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
        private readonly ILogger _logger;

        public JobsSendNewsDigest(IServiceProvider services, IOptions<BotConfig> config, ILogger logger)
            : base(services, config, logger)
        {
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken cancel, IServiceProvider scope)
        {
            var currentTime = DateTime.Now.ToString("HH:mm");

            var botService = scope.GetRequiredService<IBotService>();
            var newsClient = scope.GetRequiredService<INewsApiClient>();
            var repo = scope.GetRequiredService<IBaseRepositoryDb<UserDb>>();

            var usersCollectionDb = await repo.GetCollection(cancel);

            // Находим пользователей с непустым NewsTimes, у которых текущее время совпадает с одним из слотов
            var usersToNotify = usersCollectionDb
                .Where(u => !string.IsNullOrEmpty(u.NewsTimes))
                .Where(u => u.NewsTimes.Split(',').Any(t => t.Trim() == currentTime))
                .ToArray();

            if (!usersToNotify.Any())
                return;

            _logger.Information("News digest: found {Count} users scheduled for {Time}", usersToNotify.Length, currentTime);

            var sentCount = 0;

            foreach (var user in usersToNotify)
            {
                try
                {
                    NewsDigestResult digest;

                    if (user.LastNewsDeliveredAt.HasValue)
                    {
                        digest = await newsClient.GetDigestSinceAsync(user.LastNewsDeliveredAt.Value, 5, cancel);
                    }
                    else
                    {
                        digest = await newsClient.GetLatestDigestAsync(5, cancel);
                    }

                    if (string.IsNullOrWhiteSpace(digest?.Message) || digest.TopicIds == null || !digest.TopicIds.Any())
                    {
                        _logger.Debug("No new topics for user {ChatId}", user.ChatId);
                        continue;
                    }

                    await botService.Client.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: digest.Message,
                        parseMode: ParseMode.Markdown);

                    // Обновить время последней доставки
                    user.LastNewsDeliveredAt = DateTime.UtcNow;
                    await repo.Update(user, cancel);

                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to send news digest to user {ChatId}", user.ChatId);
                }
            }

            if (sentCount > 0)
            {
                _logger.Information("News digest sent to {Count} users at {Time}", sentCount, currentTime);
            }
        }
    }
}
