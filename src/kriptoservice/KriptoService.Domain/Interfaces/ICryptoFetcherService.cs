using KriptoService.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Domain.Interfaces
{
    public interface ICryptoFetcherService
    {
        Task<List<CryptoPriceDb>> FetchPricesAsync(CancellationToken cancel = default);
    }
}
