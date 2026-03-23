using System;
using System.Collections.Generic;

namespace KriptoService.Domain.Dto
{
    public class CryptoHistoryResponse
    {
        public string Symbol { get; set; }
        public string Currency { get; set; }
        public List<CryptoHistoryPoint> Points { get; set; } = new();
    }

    public class CryptoHistoryPoint
    {
        public decimal Price { get; set; }
        public DateTime FetchedAt { get; set; }
    }
}
