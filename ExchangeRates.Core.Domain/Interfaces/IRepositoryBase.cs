using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        Task<IEnumerable<T>> GetCollection(CancellationToken cancel);
        Task<IEnumerable<T>> GetCollection(string charCode, CancellationToken cancel);
        Task<T> GetItem(T item, CancellationToken cancel);
        Task<bool> Create(T item, CancellationToken cancel);
        Task<bool> AddCollection(List<T> listValute, CancellationToken cancel);
    }
}
