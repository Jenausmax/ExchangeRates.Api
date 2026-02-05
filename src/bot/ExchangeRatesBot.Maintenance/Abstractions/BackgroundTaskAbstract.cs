using ExchangeRatesBot.Configuration.ModelConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Maintenance.Abstractions
{
    public abstract class BackgroundTaskAbstract<T> : BackgroundService where T : BackgroundTaskAbstract<T>
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly IOptions<BotConfig> _config;

        public BackgroundTaskAbstract(IServiceProvider services, IOptions<BotConfig> config, ILogger logger)
        {
            _logger = logger;
            _services = services;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            TimeSpan per = TimeSpan.FromMinutes(1);
            while (!cancel.IsCancellationRequested)
            {
                var scope = _services.CreateScope();
                try
                {
                    await DoWorkAsync(cancel, scope.ServiceProvider);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Task failed: {typeof(T).Name}");
                }
                finally
                {
                    scope.Dispose();
                }
                await Task.Delay(per, cancel);
            }
        }

        protected abstract Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope);
    }
}
