using ExchangeRatesBot.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IBaseRepositoryDb<T> where T : Entity, new()
    {
        Task<bool> Create(T user, CancellationToken cancel = default);
        Task<T> Update(T user, CancellationToken cancel = default);
        Task<List<T>> GetCollection(CancellationToken cancel = default);
        Task<T> GetEntity(T entity, CancellationToken cancel = default);
        Task<T> GetEntityId(int id, CancellationToken cancel = default);
    }
}
