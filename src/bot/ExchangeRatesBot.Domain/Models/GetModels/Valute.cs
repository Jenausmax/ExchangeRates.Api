using System;

namespace ExchangeRatesBot.Domain.Models.GetModels
{
    public class Valute
    {
        public string Name { get; set; }
        public string CharCode { get; set; }
        public double Value { get; set; }
        public DateTime DateValute { get; set; }
        public string Difference { get; set; }
    }
}
