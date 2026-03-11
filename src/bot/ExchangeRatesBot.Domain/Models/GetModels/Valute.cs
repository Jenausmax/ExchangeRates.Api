using System;

namespace ExchangeRatesBot.Domain.Models.GetModels
{
    public class Valute
    {
        public string Name { get; set; }
        public string CharCode { get; set; }
        public double Value { get; set; }
        public DateTime DateValute { get; set; }
        public double? AbsoluteDiff { get; set; }
        public double? PercentDiff { get; set; }
    }
}
