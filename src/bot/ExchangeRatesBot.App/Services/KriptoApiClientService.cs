using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.App.Services
{
    public class KriptoApiClientService : IKriptoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public KriptoApiClientService(IOptions<BotConfig> config, ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            if (!string.IsNullOrWhiteSpace(config.Value.KriptoServiceUrl))
            {
                _httpClient.BaseAddress = new Uri(config.Value.KriptoServiceUrl);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Получить последние курсы криптовалют от KriptoService
        /// </summary>
        public async Task<CryptoPriceResult> GetLatestPricesAsync(string currency = "RUB", CancellationToken cancel = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/crypto/latest?currencies={currency}", cancel);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancel);
                return JsonSerializer.Deserialize<CryptoPriceResult>(json, _jsonOptions)
                       ?? new CryptoPriceResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get crypto prices from KriptoService");
                return new CryptoPriceResult();
            }
        }
    }
}
