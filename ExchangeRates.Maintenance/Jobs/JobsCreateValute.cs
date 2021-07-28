using ExchangeRates.Configuration;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Maintenance.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Maintenance.Jobs
{
    public class JobsCreateValute : BackgroundTaskAbstract<JobsCreateValute>
    {
        private readonly IOptions<ClientConfig> _config;
        private readonly ILogger _logger;

        public JobsCreateValute(IServiceProvider services, IOptions<ClientConfig> config, ILogger logger) 
            : base(services, config, logger)
        {
            _config = config;
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope)
        {
            DateTime timeFormat;
            DateTime timeNowFormat;
            var cur = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            DateTime.TryParse(cur, out timeNowFormat);
            DateTime.TryParse(_config.Value.TimeUpdateJobs, out timeFormat);

            if (timeFormat == timeNowFormat)
            {
                var saveService = scope.GetRequiredService<ISaveService>();
                var processingService = scope.GetRequiredService<IProcessingService>();

                try
                {
                    _logger.Information("Задача начата.", typeof(JobsCreateValute));
                    var processing = await processingService.RequestProcessing(stoppingToken);
                    var res = await saveService.SaveSet(processing, stoppingToken);
                    _logger.Information("Задача выполнена успешно.", typeof(JobsCreateValute));
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Task error: {typeof(JobsCreateValute)}");
                    throw;
                }
            }
        }
    }
}
