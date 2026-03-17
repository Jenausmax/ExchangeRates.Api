using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewsService.Configuration;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Maintenance
{
    public abstract class NewsBackgroundTask<T> : BackgroundService where T : NewsBackgroundTask<T>
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        protected readonly NewsConfig Config;

        protected NewsBackgroundTask(IServiceProvider services, IOptions<NewsConfig> config, ILogger logger)
        {
            _logger = logger;
            _services = services;
            Config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            var period = TimeSpan.FromMinutes(1);

            while (!cancel.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                try
                {
                    await DoWorkAsync(cancel, scope.ServiceProvider);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Task failed: {TaskName}", typeof(T).Name);
                }

                await Task.Delay(period, cancel);
            }
        }

        protected abstract Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope);
    }
}
