﻿using System;

namespace ExchangeRates.Core.Domain.Models.GetModel
{
    public class GetValuteModel
    {
        public string Name { get; set; }
        public string CharCode { get; set; }
        public double Value { get; set; }
        public DateTime DateSave { get; set; }
        public DateTime DateValute { get; set; }

    }
}
