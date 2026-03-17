using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsService.Configuration;
using NewsService.Domain.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Maintenance.Jobs
{
    public class JobsFetchNews : NewsBackgroundTask<JobsFetchNews>
    {
        private readonly ILogger _logger;
        private DateTime _lastFetch = DateTime.MinValue;

        public JobsFetchNews(IServiceProvider services, IOptions<NewsConfig> config, ILogger logger)
            : base(services, config, logger)
        {
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken cancel, IServiceProvider scope)
        {
            var now = DateTime.UtcNow;

            if ((now - _lastFetch).TotalMinutes < Config.FetchIntervalMinutes)
                return;

            _logger.Information("Starting RSS fetch job...");

            var fetcher = scope.GetRequiredService<IRssFetcherService>();
            var dedup = scope.GetRequiredService<INewsDeduplicationService>();

            var items = await fetcher.FetchAllFeedsAsync(cancel);
            _logger.Information("Fetched {Count} raw items from RSS feeds", items.Count);

            var saved = await dedup.DeduplicateAndSaveAsync(items, cancel);
            _logger.Information("Saved {Count} new topics after deduplication", saved.Count);

            _lastFetch = now;
        }
    }
}
