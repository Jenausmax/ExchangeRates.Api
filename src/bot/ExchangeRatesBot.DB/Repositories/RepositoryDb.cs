using ExchangeRatesBot.Domain.Interfaces;
using ExchangeRatesBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.DB.Repositories
{
    public class RepositoryDb<T> : IBaseRepositoryDb<T> where T : Entity, new()
    {
        private readonly ILogger _logger;
        private readonly DataDb _db;
        protected DbSet<T> Set { get; }

        public RepositoryDb(ILogger logger, DataDb db)
        {
            _logger = logger;
            _db = db;
            Set = _db.Set<T>();
        }
        public async Task<bool> Create(T entity, CancellationToken cancel = default)
        {
            if(entity == null)
            {
                _logger.Error($"Type error: {typeof(RepositoryDb<T>)}. Create error!");
                return false;
            }

            await Set.AddAsync(entity, cancel);
            await _db.SaveChangesAsync(cancel);
            _logger.Information($"Type info: {typeof(RepositoryDb<T>)}. Entity create! Done.");
            return true;

        }

        public async Task<List<T>> GetCollection(CancellationToken cancel = default)
        {
            return await Set.ToListAsync(cancel);
        }

        public async Task<T> GetEntity(T entity, CancellationToken cancel = default)
        {
            return await GetEntityId(entity.Id, cancel);
        }

        public async Task<T> GetEntityId(int id, CancellationToken cancel = default)
        {
            return await Set.FirstOrDefaultAsync(e => e.Id == id, cancel);
        }

        public async Task<T> Update(T entity, CancellationToken cancel = default)
        {
            var entityModify = Set.Update(entity);
            await _db.SaveChangesAsync(cancel);
            return entityModify as T;
        }
    }
}
