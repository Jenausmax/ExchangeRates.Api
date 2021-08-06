using ExchangeRates.Configuration;
using ExchangeRates.Core.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;

namespace ExchangeRates.Core.App.Services
{
    public class ApiClientService : IApiClient
    {
        public HttpClient Client { get; set; }

        public ApiClientService(IOptions<ClientConfig> config)
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri(config.Value.SiteApi);
        }
    }
}
