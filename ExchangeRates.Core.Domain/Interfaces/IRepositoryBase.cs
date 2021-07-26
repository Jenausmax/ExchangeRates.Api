using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        List<T> GetCollection { get; set; }
        Task<IEnumerable<T>> GetCollectionAll();
        Task<T> GetItem(T item);
        Task<bool> Create(T item);
        
    }
}
