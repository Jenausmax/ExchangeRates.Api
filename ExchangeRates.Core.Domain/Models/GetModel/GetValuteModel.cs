using System;

namespace ExchangeRates.Core.Domain.Models.GetModel
{
    public class GetValuteModel
    {
        public DateTime DateGet { get; set; } = DateTime.Now;
        public string Name { get; set; }
        public string CharCode { get; set; }
        public string Value { get; set; }
        public DateTime DateSave { get; set; }
        public DateTime DateValute { get; set; }

    }
}
