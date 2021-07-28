using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models.GetModel;
using ExchangeRates.Infrastructure.DB.Models;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Core.App.Services
{
    public class GetValuteService : IGetValute
    {
        private readonly IRepositoryBase<ValuteModelDb> _repository;
        private readonly ILogger _logger;

        public GetValuteService(IRepositoryBase<ValuteModelDb> repository, ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<GetValuteModel>> GetValuteDay(string charCode, CancellationToken cancel, int day = 1)
        {
            var valutes = await _repository.GetCollection(charCode, cancel);
            if (valutes.Any())
            {
                var count = valutes.Count();
                var valutesDay = valutes.OrderBy(i => i.DateSave).Skip(count - day);
                var valList = new List<GetValuteModel>();
                if (valutesDay.Any())
                {
                    foreach (var item in valutesDay)
                    {
                        var temp = new GetValuteModel()
                        {
                            Name = item.Name,
                            CharCode = item.CharCode,
                            Value = item.Value,
                            DateSave = item.DateSave,
                            DateValute = item.DateValute
                        };
                        valList.Add(temp);
                    }
                }

                return valList;
            }
            _logger.Error($"Type error: {typeof(GetValuteService)} Collection null");
            return null;
        }
    }
}
