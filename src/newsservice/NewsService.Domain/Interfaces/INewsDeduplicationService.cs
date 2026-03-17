using NewsService.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface INewsDeduplicationService
    {
        Task<List<NewsTopicDb>> DeduplicateAndSaveAsync(List<RssNewsItem> items, CancellationToken cancel = default);
    }
}
