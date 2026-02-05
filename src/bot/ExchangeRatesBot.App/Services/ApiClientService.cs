using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace ExchangeRatesBot.App.Services
{
    public class ApiClientService : IApiClient
    {
        public HttpClient Client { get; set; }

        public ApiClientService(IOptions<BotConfig> config)
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri(config.Value.UrlRequest);
        }
    }
}
