using System.Collections.Generic;

namespace NewsService.Domain.Dto
{
    public class DigestResponse
    {
        public string Message { get; set; }
        public List<int> TopicIds { get; set; }
        public bool HasMore { get; set; }
    }
}
