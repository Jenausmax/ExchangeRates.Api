using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeRates.Core.Domain.Models;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IProcessingService
    {
        Task<Root> RequestProcessing();
    }
}
