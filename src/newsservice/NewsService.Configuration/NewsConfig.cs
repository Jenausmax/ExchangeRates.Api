namespace NewsService.Configuration
{
    public class NewsConfig
    {
        public bool Enabled { get; set; } = true;
        public int FetchIntervalMinutes { get; set; } = 60;
        public string SendTime { get; set; } = "09:00";
        public int MaxNewsPerDigest { get; set; } = 5;
        public string[] RssFeeds { get; set; } = new[]
        {
            "https://www.cbr.ru/rss/RssPress",
            "https://www.cbr.ru/rss/eventrss"
        };
    }
}
