using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models.GetModel;

namespace ExchangeRates.Core.App.Services
{
    public class GetValuteService : IGetValute
    {
        public GetValuteService()
        {
            
        }
        public async Task<List<GetValuteModel>> GetValute(CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }
}
