using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IKriptoApiClient
    {
        /// <summary>
        /// Получить последние курсы криптовалют от KriptoService
        /// </summary>
        Task<CryptoPriceResult> GetLatestPricesAsync(string currency = "RUB", CancellationToken cancel = default);
    }

    public class CryptoPriceResult
    {
        public List<CryptoPriceItem> Prices { get; set; } = new();
        public DateTime FetchedAt { get; set; }
    }

    public class CryptoPriceItem
    {
        public string Symbol { get; set; }
        public string Currency { get; set; }
        public decimal Price { get; set; }
        public decimal Change24h { get; set; }
        public decimal ChangePct24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public decimal Volume24h { get; set; }
        public decimal MarketCap { get; set; }
    }
}
