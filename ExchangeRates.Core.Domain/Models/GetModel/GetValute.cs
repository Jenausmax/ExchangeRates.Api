using System;
using System.Collections.Generic;

namespace ExchangeRates.Core.Domain.Models.GetModel
{
    public class GetValute
    {
        public DateTime DateGet { get; set; } = DateTime.Now;
        public List<GetValuteModel> GetValuteModels { get; set; }
    }
}
