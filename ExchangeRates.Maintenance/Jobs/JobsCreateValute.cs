using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRates.Configuration;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Maintenance.Abstraction;
using Microsoft.Extensions.Options;
using Serilog;

namespace ExchangeRates.Maintenance.Jobs
{
    public class JobsCreateValute : BackgroundTaskAbstract<JobsCreateValute>
    {
        private readonly ILogger _logger;
        private readonly ISaveService _saveService;
        private readonly IProcessingService _processing;

        public JobsCreateValute(IServiceProvider services, TimeSpan period, ILogger logger, IOptions<ClientConfig> config, ISaveService saveService, IProcessingService processing) 
            : base(services, TimeSpan.FromHours(config.Value.PeriodHours), logger)
        {
            _logger = logger;
            _saveService = saveService;
            _processing = processing;
        }

        protected override async Task DoWorkAsync(CancellationToken stoppingToken, IServiceProvider scope)
        {
            
            try
            {
                _logger.Information("Задача начата.", typeof(JobsCreateValute));
                var processing = await _processing.RequestProcessing();
                var res = await _saveService.SaveSet(processing, stoppingToken);
                _logger.Information("Задача выполнена успешно.", typeof(JobsCreateValute));
            }
            catch (Exception e)
            {
                _logger.Error(e,$"Task error: {typeof(JobsCreateValute)}");
                throw;
            }
        }
    }
}
