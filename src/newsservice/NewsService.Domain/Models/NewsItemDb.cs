using System;

namespace NewsService.Domain.Models
{
    public class NewsItemDb : Entity
    {
        public int TopicId { get; set; }
        public NewsTopicDb Topic { get; set; }
        public string RawTitle { get; set; }
        public string RawDescription { get; set; }
        public string RawUrl { get; set; }
        public DateTime RawPublishedAt { get; set; }
        public string SourceFeed { get; set; }
    }
}
