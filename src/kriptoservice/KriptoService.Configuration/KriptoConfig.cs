namespace KriptoService.Configuration
{
    public class KriptoConfig
    {
        public bool Enabled { get; set; } = true;
        public int FetchIntervalMinutes { get; set; } = 5;
        public string ApiUrl { get; set; } = "https://min-api.cryptocompare.com/";
        public string ApiKey { get; set; } = "";
        public string[] Symbols { get; set; } = new[] { "BTC", "ETH", "SOL", "XRP", "BNB", "USDT", "DOGE", "ADA", "TON", "AVAX" };
        public string[] Currencies { get; set; } = new[] { "RUB", "USD" };
        public int HistoryRetentionDays { get; set; } = 180;
    }
}
