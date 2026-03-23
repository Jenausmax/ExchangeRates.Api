using Microsoft.EntityFrameworkCore;
using KriptoService.Domain.Interfaces;
using KriptoService.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.DB.Repositories
{
    public class CryptoRepository : ICryptoRepository
    {
        private readonly KriptoDataDb _db;
        private readonly ILogger _logger;

        public CryptoRepository(KriptoDataDb db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SavePricesAsync(List<CryptoPriceDb> prices, CancellationToken cancel = default)
        {
            await _db.Prices.AddRangeAsync(prices, cancel);
            await _db.SaveChangesAsync(cancel);
        }

        public async Task<List<CryptoPriceDb>> GetLatestPricesAsync(string[] symbols, string[] currencies, CancellationToken cancel = default)
        {
            // Находим время последнего фетча
            var lastFetch = await _db.Prices
                .OrderByDescending(p => p.FetchedAt)
                .Select(p => p.FetchedAt)
                .FirstOrDefaultAsync(cancel);

            if (lastFetch == default)
                return new List<CryptoPriceDb>();

            // Берём все записи последнего батча (допуск 5 секунд)
            var cutoff = lastFetch.AddSeconds(-5);
            var query = _db.Prices.Where(p => p.FetchedAt >= cutoff);

            if (symbols != null && symbols.Length > 0)
            {
                var symbolList = symbols.ToList();
                query = query.Where(p => symbolList.Contains(p.Symbol));
            }

            if (currencies != null && currencies.Length > 0)
            {
                var currencyList = currencies.ToList();
                query = query.Where(p => currencyList.Contains(p.Currency));
            }

            return await query
                .OrderBy(p => p.Symbol)
                .ThenBy(p => p.Currency)
                .ToListAsync(cancel);
        }

        public async Task<List<CryptoPriceDb>> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default)
        {
            var since = DateTime.UtcNow.AddHours(-hours);
            return await _db.Prices
                .Where(p => p.Symbol == symbol && p.Currency == currency && p.FetchedAt >= since)
                .OrderBy(p => p.FetchedAt)
                .ToListAsync(cancel);
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancel = default)
        {
            return await _db.Prices.CountAsync(cancel);
        }

        public async Task<DateTime?> GetLastFetchTimeAsync(CancellationToken cancel = default)
        {
            if (!await _db.Prices.AnyAsync(cancel))
                return null;
            return await _db.Prices.MaxAsync(p => p.FetchedAt, cancel);
        }

        public async Task<int> CleanupOldRecordsAsync(int retentionDays, CancellationToken cancel = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var deleted = await _db.Prices
                .Where(p => p.FetchedAt < cutoff)
                .ExecuteDeleteAsync(cancel);

            if (deleted > 0)
            {
                _logger.Information("Cleaned up {Count} old crypto price records", deleted);
            }

            return deleted;
        }
    }
}
