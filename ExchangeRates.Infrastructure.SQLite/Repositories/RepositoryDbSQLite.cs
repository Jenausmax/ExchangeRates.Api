using ExchangeRates.Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRates.Infrastructure.SQLite.Repositories
{
    public class RepositoryDbSQLite<T> : IRepositoryBase<T> where T : class
    {
        public List<T> GetCollection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<bool> Create(T item)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetCollectionAll()
        {
            throw new NotImplementedException();
        }

        public Task<T> GetItem(T item)
        {
            throw new NotImplementedException();
        }
    }
}
