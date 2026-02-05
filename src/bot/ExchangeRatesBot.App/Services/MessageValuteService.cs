using ExchangeRatesBot.Domain.Interfaces;
using ExchangeRatesBot.Domain.Models.GetModels;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.App.Services
{
    public class MessageValuteService : IMessageValute
    {
        private readonly IProcessingService _processingService;
        private readonly ILogger _logger;

        public MessageValuteService(IProcessingService processingService, ILogger logger)
        {
            _processingService = processingService;
            _logger = logger;
        }

        public async Task<string> GetValuteMessage(int day, string charCode, CancellationToken cancel)
        {
            var valutesRoot = await _processingService.RequestProcessing(day, charCode, cancel);

            if (valutesRoot == null || day == 0)
            {
                _logger.Error($"Type error: {typeof(MessageValuteService)}.: Collection null");
                return " ";
            }

            var getValutesModels = valutesRoot.GetValuteModels;
            var valutes = new List<Valute>();
            foreach (var item in getValutesModels)
            {
                
                valutes.Add(new Valute()
                {
                    CharCode = item.CharCode,
                    DateValute = item.DateValute,
                    Name = item.Name,
                    Value = item.Value
                });
            }

            #region высчитываем Difference

            if (day >= 3)
            {
                for (int i = 0; i < valutes.Count; i++)
                {
                    if (i != valutes.Count - 1)
                    {
                        if (valutes[i].Value > valutes[i + 1].Value)
                        {
                            var temp = valutes[i].Value - valutes[i + 1].Value;
                            valutes[i + 1].Difference = $"(- *{string.Format("{0:0.00}", temp)})*";
                        }
                        else
                        {
                            var temp = valutes[i + 1].Value - valutes[i].Value;
                            valutes[i + 1].Difference = $"(+ *{string.Format("{0:0.00}", temp)})*";
                        }
                    }
                }
            }
            #endregion

            string res = $"*{valutes[0].Name}* \n\r {valutes[0].CharCode}/RUB \n\r  \n\r";
            var v = valutes;
            if (day >= 3)
            {
                v = valutes
                    .GroupBy(e => e.DateValute)
                    .Select(g => g.First())
                    .Skip(1).ToList();
            }
            else
            {
                v = valutes
                    .GroupBy(e => e.DateValute)
                    .Select(g => g.First())
                    .ToList();
            }

            

            foreach (var valute in v)
            {
                res = res + $" {valute.DateValute} ---> {valute.Value}  {valute.Difference} \n\r ";
            }

            return res;
        }

        public async Task<string> GetValuteMessage(int day, string[] charCodesCollection, CancellationToken cancel)
        {
            string result = "";

            foreach (var item in charCodesCollection)
            {
                var valuteString = await GetValuteMessage(day, item, cancel);
                result = result + valuteString + "\n\r";
            }
            return result;
        }
    }
}
