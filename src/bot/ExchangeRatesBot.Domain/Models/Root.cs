using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeRatesBot.Domain.Models
{
    public class Root
    {
        [JsonPropertyName("dateGet")]
        public DateTime DateGet { get; set; }

        [JsonPropertyName("getValuteModels")]
        public List<GetValuteModel> GetValuteModels { get; set; }
    }

    public sealed class GetValuteModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("charCode")]
        public string CharCode { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("dateSave")]
        public DateTime DateSave { get; set; }

        [JsonPropertyName("dateValute")]
        public DateTime DateValute { get; set; }
    }
}
