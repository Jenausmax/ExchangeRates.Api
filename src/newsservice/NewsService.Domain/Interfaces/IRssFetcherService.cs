using NewsService.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface IRssFetcherService
    {
        Task<List<RssNewsItem>> FetchAllFeedsAsync(CancellationToken cancel = default);
    }
}
