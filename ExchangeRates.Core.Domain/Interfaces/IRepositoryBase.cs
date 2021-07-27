﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        Task<IEnumerable<T>> GetCollection(CancellationToken cancel);
        Task<T> GetItem(T item, CancellationToken cancel);
        Task<bool> Create(T item, CancellationToken cancel);
        
    }
}