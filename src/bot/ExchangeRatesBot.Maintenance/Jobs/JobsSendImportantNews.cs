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
using Telegram.Bot.Types.Enums;

namespace ExchangeRatesBot.Maintenance.Jobs
{
    public class JobsSendImportantNews : BackgroundTaskAbstract<JobsSendImportantNews>
    {
        private readonly ILogger _logger;

        public JobsSendImportantNews(IServiceProvider services, IOptions<BotConfig> config, ILogger logger)
            : base(services, config, logger)
        {
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken cancel, IServiceProvider scope)
        {
            var currentTime = DateTime.Now.ToString("HH:mm");

            // Срабатываем только в начале каждого часа (XX:00)
            if (!currentTime.EndsWith(":00"))
                return;

            var botService = scope.GetRequiredService<IBotService>();
            var newsClient = scope.GetRequiredService<INewsApiClient>();
            var repo = scope.GetRequiredService<IBaseRepositoryDb<UserDb>>();

            var usersCollectionDb = await repo.GetCollection(cancel);

            var usersToNotify = usersCollectionDb
                .Where(u => u.ImportantNewsSubscribe)
                .ToArray();

            if (!usersToNotify.Any())
                return;

            // Получить самую важную новость один раз для всех
            var digest = await newsClient.GetMostImportantAsync(cancel);

            if (string.IsNullOrWhiteSpace(digest?.Message) || digest.TopicIds == null || !digest.TopicIds.Any())
            {
                _logger.Debug("No important news to send");
                return;
            }

            _logger.Information("Important news: sending to {Count} users at {Time}", usersToNotify.Length, currentTime);

            var sentCount = 0;

            foreach (var user in usersToNotify)
            {
                try
                {
                    await botService.Client.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: digest.Message,
                        parseMode: ParseMode.Markdown);

                    user.LastImportantNewsAt = DateTime.UtcNow;
                    await repo.Update(user, cancel);

                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to send important news to user {ChatId}", user.ChatId);
                }
            }

            if (sentCount > 0)
            {
                _logger.Information("Important news sent to {Count} users", sentCount);
                await newsClient.MarkSentAsync(digest.TopicIds, cancel);
                _logger.Information("Marked {Count} topics as sent", digest.TopicIds.Count);
            }
        }
    }
}
