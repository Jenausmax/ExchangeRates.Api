using System;
using System.Collections.Generic;

namespace KriptoService.Domain.Dto
{
    public class CryptoPriceResponse
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
