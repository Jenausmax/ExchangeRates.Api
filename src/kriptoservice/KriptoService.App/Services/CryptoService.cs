using KriptoService.Configuration;
using KriptoService.Domain.Dto;
using KriptoService.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.App.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly ICryptoRepository _repository;
        private readonly KriptoConfig _config;
        private readonly ILogger _logger;

        public CryptoService(ICryptoRepository repository, IOptions<KriptoConfig> config, ILogger logger)
        {
            _repository = repository;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<CryptoPriceResponse> GetLatestAsync(string[] symbols, string[] currencies, CancellationToken cancel = default)
        {
            var effectiveSymbols = symbols?.Length > 0 ? symbols : _config.Symbols;
            var effectiveCurrencies = currencies?.Length > 0 ? currencies : _config.Currencies;

            var prices = await _repository.GetLatestPricesAsync(effectiveSymbols, effectiveCurrencies, cancel);

            return new CryptoPriceResponse
            {
                Prices = prices.Select(p => new CryptoPriceItem
                {
                    Symbol = p.Symbol,
                    Currency = p.Currency,
                    Price = p.Price,
                    Change24h = p.Change24h,
                    ChangePct24h = p.ChangePct24h,
                    High24h = p.High24h,
                    Low24h = p.Low24h,
                    Volume24h = p.Volume24h,
                    MarketCap = p.MarketCap
                }).ToList(),
                FetchedAt = prices.FirstOrDefault()?.FetchedAt ?? default
            };
        }

        public async Task<CryptoHistoryResponse> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default)
        {
            var history = await _repository.GetHistoryAsync(symbol, currency, hours, cancel);

            return new CryptoHistoryResponse
            {
                Symbol = symbol,
                Currency = currency,
                Points = history.Select(p => new CryptoHistoryPoint
                {
                    Price = p.Price,
                    FetchedAt = p.FetchedAt
                }).ToList()
            };
        }

        public async Task<CryptoStatusResponse> GetStatusAsync(CancellationToken cancel = default)
        {
            return new CryptoStatusResponse
            {
                TotalRecords = await _repository.GetTotalCountAsync(cancel),
                LastFetchTime = await _repository.GetLastFetchTimeAsync(cancel),
                TrackedSymbols = _config.Symbols,
                TrackedCurrencies = _config.Currencies
            };
        }
    }
}
