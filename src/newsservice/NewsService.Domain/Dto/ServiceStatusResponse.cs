using System;

namespace NewsService.Domain.Dto
{
    public class ServiceStatusResponse
    {
        public int TotalTopics { get; set; }
        public int UnsentTopics { get; set; }
        public DateTime? LastFetchTime { get; set; }
    }
}
