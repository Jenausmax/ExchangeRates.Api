using KriptoService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Domain.Interfaces
{
    public interface ICryptoRepository
    {
        Task SavePricesAsync(List<CryptoPriceDb> prices, CancellationToken cancel = default);
        Task<List<CryptoPriceDb>> GetLatestPricesAsync(string[] symbols, string[] currencies, CancellationToken cancel = default);
        Task<List<CryptoPriceDb>> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default);
        Task<int> GetTotalCountAsync(CancellationToken cancel = default);
        Task<DateTime?> GetLastFetchTimeAsync(CancellationToken cancel = default);
        Task<int> CleanupOldRecordsAsync(int retentionDays, CancellationToken cancel = default);
    }
}
