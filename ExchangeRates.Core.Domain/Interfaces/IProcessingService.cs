using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IProcessingService
    {
        Task RequestProcessing();
    }
}
