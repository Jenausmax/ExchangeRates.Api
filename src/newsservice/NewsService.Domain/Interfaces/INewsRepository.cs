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
        Task<List<NewsTopicDb>> GetTopicsSinceAsync(DateTime since, int maxCount, CancellationToken cancel = default);
        Task<List<NewsTopicDb>> GetTopicsBeforeIdAsync(int beforeId, int maxCount, CancellationToken cancel = default);
        Task<List<NewsTopicDb>> GetAllTopicsAsync(int maxCount, CancellationToken cancel = default);
        Task<List<NewsTopicDb>> GetRecentTopicsForSimilarityAsync(int hoursBack = 48, CancellationToken cancel = default);
        Task<NewsTopicDb> GetMostImportantUnsentAsync(int maxAgeHours = 0, int minSourceCount = 0, CancellationToken cancel = default);
        Task IncrementSourceCountAsync(int topicId, CancellationToken cancel = default);
    }
}
