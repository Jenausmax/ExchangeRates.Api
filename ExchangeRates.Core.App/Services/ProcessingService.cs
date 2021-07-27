using ExchangeRates.Configuration;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExchangeRates.Core.App.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IApiClient _client;
        private readonly IOptions<ClientConfig> _config;
        private readonly ILogger _logger;

        public ProcessingService(IApiClient apiClient, 
            IOptions<ClientConfig> config, 
            ILogger logger)
        {
            _client = apiClient;
            _config = config;
            _logger = logger;
        }

        public async Task<Root> RequestProcessing()
        {
            try
            {
                _logger.Information("Обращение к методу RequestProcessing()");
                var resp = await _client.Client.GetAsync(_config.Value.SiteGet);
                var resultContent = await resp.Content.ReadAsStreamAsync();
                var res = await JsonSerializer.DeserializeAsync<Root>(resultContent);
                _logger.Information("Deserialize succes");
                return res;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error: Deserialize");
                Console.WriteLine(e);
                throw;
            }
            
        }
    }
}
