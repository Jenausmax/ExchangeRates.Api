using KriptoService.Configuration;
using KriptoService.Domain.Interfaces;
using KriptoService.Domain.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.App.Services
{
    public class CryptoCompareFetcherService : ICryptoFetcherService
    {
        private readonly KriptoConfig _config;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public CryptoCompareFetcherService(HttpClient httpClient, IOptions<KriptoConfig> config, ILogger logger)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<List<CryptoPriceDb>> FetchPricesAsync(CancellationToken cancel = default)
        {
            var symbols = string.Join(",", _config.Symbols ?? System.Array.Empty<string>());
            var currencies = string.Join(",", _config.Currencies ?? System.Array.Empty<string>());
            var url = $"{_config.ApiUrl.TrimEnd('/')}/data/pricemultifull?fsyms={symbols}&tsyms={currencies}";

            _logger.Information("Fetching crypto prices from CryptoCompare: {Symbols} in {Currencies}", symbols, currencies);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                request.Headers.Add("authorization", $"Apikey {_config.ApiKey}");
            }

            var httpResponse = await _httpClient.SendAsync(request, cancel);
            httpResponse.EnsureSuccessStatusCode();
            var response = await httpResponse.Content.ReadAsStringAsync(cancel);
            var json = JObject.Parse(response);

            var raw = json["RAW"];
            if (raw == null)
            {
                _logger.Warning("CryptoCompare response has no RAW data. Response: {Response}", response.Substring(0, Math.Min(500, response.Length)));
                return new List<CryptoPriceDb>();
            }

            var prices = new List<CryptoPriceDb>();
            var fetchedAt = DateTime.UtcNow;

            foreach (var symbolToken in raw.Children<JProperty>())
            {
                var symbol = symbolToken.Name;
                foreach (var currencyToken in symbolToken.Value.Children<JProperty>())
                {
                    var currency = currencyToken.Name;
                    var data = currencyToken.Value;

                    prices.Add(new CryptoPriceDb
                    {
                        Symbol = symbol,
                        Currency = currency,
                        Price = data.Value<decimal>("PRICE"),
                        Change24h = data.Value<decimal>("CHANGE24HOUR"),
                        ChangePct24h = data.Value<decimal>("CHANGEPCT24HOUR"),
                        High24h = data.Value<decimal>("HIGH24HOUR"),
                        Low24h = data.Value<decimal>("LOW24HOUR"),
                        Volume24h = data.Value<decimal>("TOTALVOLUME24HTO"),
                        MarketCap = data.Value<decimal>("MKTCAP"),
                        FetchedAt = fetchedAt
                    });
                }
            }

            _logger.Information("Fetched {Count} crypto price records", prices.Count);
            return prices;
        }
    }
}
