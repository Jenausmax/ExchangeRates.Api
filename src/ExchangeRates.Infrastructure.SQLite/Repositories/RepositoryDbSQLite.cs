using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Infrastructure.DB;
using ExchangeRates.Infrastructure.DB.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Infrastructure.SQLite.Repositories
{
    public class RepositoryDbSQLite<T> : IRepositoryBase<T> where T : ValuteModelDb, new()
    {
        private readonly DataDb _db;
        private readonly ILogger _logger;

        protected DbSet<T> Set { get; }

        public RepositoryDbSQLite(DataDb db, ILogger logger)
        {
            _db = db;
            _logger = logger;
            Set = _db.Set<T>();
        }

        public async Task<bool> Create(T item, CancellationToken cancel)
        {
            await Set.AddAsync(item, cancel);
            await _db.SaveChangesAsync(cancel);
            _logger.Information("Item created.");
            return true;
        }

        public async Task<bool> AddCollection(List<T> items, CancellationToken cancel)
        {
            foreach (var item in items)
            {
                await Create(item, cancel);
            }
            _logger.Information("Items added.");
            return true;
        }

        public async Task<IEnumerable<T>> GetCollection(CancellationToken cancel)
        {
            return await Set.ToArrayAsync(cancel);
        }

        public async Task<IEnumerable<T>> GetCollection(string charCode, CancellationToken cancel)
        {
            return await Set.Where(i => i.CharCode == charCode).ToListAsync(cancel);
        }

        public async Task<T> GetItem(T item, CancellationToken cancel)
        {
            return await Set.FirstOrDefaultAsync(i => i.Id == item.Id, cancel);
        }
    }
}
