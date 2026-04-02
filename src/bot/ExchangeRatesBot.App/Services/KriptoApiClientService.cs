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
        public async Task<CryptoPriceResult> GetLatestPricesAsync(string currency = "RUB", string symbols = null, CancellationToken cancel = default)
        {
            try
            {
                var url = $"api/crypto/latest?currencies={currency}";
                if (!string.IsNullOrEmpty(symbols))
                {
                    url += $"&symbols={symbols}";
                }
                var response = await _httpClient.GetAsync(url, cancel);
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

        /// <summary>
        /// Получить историю цен одной криптовалюты за N часов
        /// </summary>
        public async Task<CryptoHistoryResult> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default)
        {
            try
            {
                var url = $"api/crypto/history?symbol={symbol}&currency={currency}&hours={hours}";
                var response = await _httpClient.GetAsync(url, cancel);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancel);
                return JsonSerializer.Deserialize<CryptoHistoryResult>(json, _jsonOptions)
                       ?? new CryptoHistoryResult { Symbol = symbol, Currency = currency };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get crypto history for {Symbol}/{Currency}", symbol, currency);
                return new CryptoHistoryResult { Symbol = symbol, Currency = currency };
            }
        }
    }
}
