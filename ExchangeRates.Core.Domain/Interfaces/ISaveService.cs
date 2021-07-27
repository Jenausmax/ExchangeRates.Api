using ExchangeRates.Core.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface ISaveService
    {
        Task<bool> SaveSet(Root item, CancellationToken cancel);
    }
}
