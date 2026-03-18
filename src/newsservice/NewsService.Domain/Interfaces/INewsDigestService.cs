using NewsService.Domain.Dto;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface INewsDigestService
    {
        Task<DigestResponse> GetLatestDigestAsync(int maxNews, bool all = false, CancellationToken cancel = default);
        Task<DigestResponse> GetDigestSinceAsync(DateTime since, int maxNews, CancellationToken cancel = default);
        Task<DigestResponse> GetDigestBeforeIdAsync(int beforeId, int maxNews, CancellationToken cancel = default);
        Task<int> MarkAsSentAsync(List<int> topicIds, CancellationToken cancel = default);
        Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancel = default);
    }
}
