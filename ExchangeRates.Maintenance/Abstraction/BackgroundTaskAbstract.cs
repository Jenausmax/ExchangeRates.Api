using ExchangeRates.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Maintenance.Abstraction
{
    public abstract class BackgroundTaskAbstract<T> : BackgroundService 
        where T : BackgroundTaskAbstract<T>
    {
        private readonly IServiceProvider _services;
        private readonly IOptions<ClientConfig> _period;
        private readonly ILogger _logger;

        public BackgroundTaskAbstract(IServiceProvider services, IOptions<ClientConfig> period, ILogger logger)
        {
            _period = period;
            _logger = logger;
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan per = TimeSpan.FromMinutes(_period.Value.PeriodMinute);
            while (!stoppingToken.IsCancellationRequested)
            {
                var scope = _services.CreateScope();
                try
                {
                    await DoWorkAsync(stoppingToken, scope.ServiceProvider);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Task failed: {typeof(T).Name}");
                }
                finally
                {
                    scope.Dispose();
                }
                await Task.Delay(per, stoppingToken);
            }
        }

        protected abstract Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope);
    }
}
