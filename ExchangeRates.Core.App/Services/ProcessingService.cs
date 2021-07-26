using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExchangeRates.Configuration;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models;
using Microsoft.Extensions.Options;

namespace ExchangeRates.Core.App.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IApiClient _client;
        private readonly IOptions<ClientConfig> _config;

        public ProcessingService(IApiClient apiClient, IOptions<ClientConfig> config)
        {
            _client = apiClient;
            _config = config;
        }

        public async Task RequestProcessing()
        {
            try
            {
                var resp = await _client.Client.GetAsync(_config.Value.SiteGet);
                var resultContent = await resp.Content.ReadAsStreamAsync();
                var res = await JsonSerializer.DeserializeAsync<Root>(resultContent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }
}
