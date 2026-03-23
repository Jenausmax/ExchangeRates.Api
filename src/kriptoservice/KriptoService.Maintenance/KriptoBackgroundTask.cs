using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using KriptoService.Configuration;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Maintenance
{
    public abstract class KriptoBackgroundTask<T> : BackgroundService where T : KriptoBackgroundTask<T>
    {
        protected readonly ILogger Logger;
        private readonly IServiceProvider _services;
        protected readonly KriptoConfig Config;

        protected KriptoBackgroundTask(IServiceProvider services, IOptions<KriptoConfig> config, ILogger logger)
        {
            Logger = logger;
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
                    Logger.Error(e, "Task failed: {TaskName}", typeof(T).Name);
                }

                await Task.Delay(period, cancel);
            }
        }

        protected abstract Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope);
    }
}
