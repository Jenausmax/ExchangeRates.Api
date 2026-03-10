namespace ExchangeRatesBot.Configuration.ModelConfig
{
    public class BotConfig
    {
        public string BotToken { get; set; }
        public string Webhook { get; set; }

        /// <summary>
        /// Режим работы бота: true - polling, false - webhook
        /// По умолчанию false (webhook режим для обратной совместимости)
        /// </summary>
        public bool UsePolling { get; set; } = false;

        public string UrlRequest { get; set; }
        public string TimeOne { get; set; }
        public string TimeTwo { get; set; }
    }
}
