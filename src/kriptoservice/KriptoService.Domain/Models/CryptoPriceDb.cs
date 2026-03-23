using System;
using System.ComponentModel.DataAnnotations;

namespace KriptoService.Domain.Models
{
    public class CryptoPriceDb : Entity
    {
        [Required]
        public string Symbol { get; set; }
        [Required]
        public string Currency { get; set; }
        public decimal Price { get; set; }
        public decimal Change24h { get; set; }
        public decimal ChangePct24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public decimal Volume24h { get; set; }
        public decimal MarketCap { get; set; }
        public DateTime FetchedAt { get; set; }
    }
}
