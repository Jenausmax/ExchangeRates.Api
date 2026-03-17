using NewsService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface INewsRepository
    {
        Task<bool> ExistsByHashAsync(string contentHash, CancellationToken cancel = default);
        Task<NewsTopicDb> CreateTopicAsync(NewsTopicDb topic, CancellationToken cancel = default);
        Task<NewsItemDb> CreateItemAsync(NewsItemDb item, CancellationToken cancel = default);
        Task<List<NewsTopicDb>> GetUnsentTopicsAsync(int maxCount, CancellationToken cancel = default);
        Task<int> MarkTopicsAsSentAsync(List<int> topicIds, CancellationToken cancel = default);
        Task<int> GetTotalCountAsync(CancellationToken cancel = default);
        Task<int> GetUnsentCountAsync(CancellationToken cancel = default);
        Task<DateTime?> GetLastFetchTimeAsync(CancellationToken cancel = default);
    }
}
