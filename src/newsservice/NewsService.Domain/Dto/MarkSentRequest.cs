using System.Collections.Generic;

namespace NewsService.Domain.Dto
{
    public class MarkSentRequest
    {
        public List<int> TopicIds { get; set; }
    }
}
