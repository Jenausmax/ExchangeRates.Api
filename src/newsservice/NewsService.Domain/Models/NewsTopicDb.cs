using System;

namespace NewsService.Domain.Models
{
    public class NewsTopicDb : Entity
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Url { get; set; }
        public string Source { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime FetchedAt { get; set; }
        public bool IsSent { get; set; }
        public string ContentHash { get; set; }
    }
}
