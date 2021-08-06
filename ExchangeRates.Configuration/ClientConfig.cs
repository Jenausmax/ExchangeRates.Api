namespace ExchangeRates.Configuration
{
    public class ClientConfig
    {
        public string SiteApi { get; set; }
        public string SiteGet { get; set; }
        public int PeriodMinute { get; set; }
        public string TimeUpdateJobs { get; set; }
        public bool JobsValute { get; set; }
        public bool JobsValuteToHour { get; set; }


    }
}
