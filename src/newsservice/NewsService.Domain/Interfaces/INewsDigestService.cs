using NewsService.Domain.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface INewsDigestService
    {
        Task<DigestResponse> GetLatestDigestAsync(int maxNews, CancellationToken cancel = default);
        Task<int> MarkAsSentAsync(List<int> topicIds, CancellationToken cancel = default);
        Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancel = default);
    }
}
