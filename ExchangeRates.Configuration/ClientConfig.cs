namespace ExchangeRates.Configuration
{
    public class ClientConfig
    {
        public string SiteApi { get; set; }
        public string SiteGet { get; set; }
        public bool Logging { get; set; }
        public int PeriodMinute { get; set; }
        public string TimeUpdateJobs { get; set; }
        
    }
}
