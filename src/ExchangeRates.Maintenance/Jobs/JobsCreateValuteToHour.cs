using ExchangeRates.Configuration;
using ExchangeRates.Maintenance.Abstraction;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRates.Core.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeRates.Maintenance.Jobs
{
    public  class JobsCreateValuteToHour : BackgroundTaskAbstract<JobsCreateValute>
    {
        private readonly IOptions<ClientConfig> _period;
        private readonly ILogger _logger;

        public JobsCreateValuteToHour(IServiceProvider services, IOptions<ClientConfig> period, ILogger logger) : base(services, period, logger)
        {
            _period = period;
            _logger = logger;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope)
        {
            var saveService = scope.GetRequiredService<ISaveService>();
            var processingService = scope.GetRequiredService<IProcessingService>();

            try
            {
                _logger.Information("Task run.", typeof(JobsCreateValuteToHour));
                var processing = await processingService.RequestProcessing(stoppingToken);
                var res = await saveService.SaveSetNoDublicate(processing, stoppingToken);
                _logger.Information("Task succesful.", typeof(JobsCreateValuteToHour));
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Task error: {typeof(JobsCreateValute)}");
                throw;
            }
        }
    }
}
