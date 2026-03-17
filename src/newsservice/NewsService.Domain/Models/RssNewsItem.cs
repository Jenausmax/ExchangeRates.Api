using System;

namespace NewsService.Domain.Models
{
    public class RssNewsItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public DateTime PublishedAt { get; set; }
        public string SourceFeed { get; set; }
    }
}
