namespace NewsService.Configuration
{
    public class NewsConfig
    {
        public bool Enabled { get; set; } = true;
        public int FetchIntervalMinutes { get; set; } = 30;
        public double SimilarityThreshold { get; set; } = 0.5;
        public string SendTime { get; set; } = "09:00";
        public int MaxNewsPerDigest { get; set; } = 5;
        public int ImportantNewsMaxAgeHours { get; set; } = 2;
        public string[] RssFeeds { get; set; } = new[]
        {
            "https://www.cbr.ru/rss/RssPress",
            "https://www.cbr.ru/rss/eventrss",
            "https://rssexport.rbc.ru/rbcnews/news/30/full.rss",
            "https://www.interfax.ru/rss.asp",
            "https://tass.ru/rss/v2.xml",
            "https://www.kommersant.ru/RSS/section-economics.xml",
            "https://www.banki.ru/xml/news.rss"
        };
    }
}
