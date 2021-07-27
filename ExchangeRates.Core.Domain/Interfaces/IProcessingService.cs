using ExchangeRates.Core.Domain.Models;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IProcessingService
    {
        Task<Root> RequestProcessing();
    }
}
