using System;

namespace KriptoService.Domain.Dto
{
    public class CryptoStatusResponse
    {
        public int TotalRecords { get; set; }
        public DateTime? LastFetchTime { get; set; }
        public string[] TrackedSymbols { get; set; }
        public string[] TrackedCurrencies { get; set; }
    }
}
