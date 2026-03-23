using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KriptoService.Configuration;
using KriptoService.Domain.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Maintenance.Jobs
{
    public class JobsFetchCrypto : KriptoBackgroundTask<JobsFetchCrypto>
    {
        private DateTime _lastFetch = DateTime.MinValue;
        private DateTime _lastCleanup = DateTime.MinValue;

        public JobsFetchCrypto(IServiceProvider services, IOptions<KriptoConfig> config, ILogger logger)
            : base(services, config, logger)
        {
        }

        protected override async Task DoWorkAsync(CancellationToken cancel, IServiceProvider scope)
        {
            var now = DateTime.UtcNow;

            if ((now - _lastFetch).TotalMinutes < Config.FetchIntervalMinutes)
                return;

            Logger.Information("Starting crypto price fetch job...");

            var fetcher = scope.GetRequiredService<ICryptoFetcherService>();
            var repo = scope.GetRequiredService<ICryptoRepository>();

            var prices = await fetcher.FetchPricesAsync(cancel);
            if (prices.Count > 0)
            {
                await repo.SavePricesAsync(prices, cancel);
                Logger.Information("Saved {Count} crypto price records", prices.Count);
            }

            _lastFetch = now;

            // Очистка старых записей раз в сутки
            if ((now - _lastCleanup).TotalHours >= 24)
            {
                var cleaned = await repo.CleanupOldRecordsAsync(Config.HistoryRetentionDays, cancel);
                if (cleaned > 0)
                    Logger.Information("Cleanup: removed {Count} old records", cleaned);
                _lastCleanup = now;
            }
        }
    }
}
