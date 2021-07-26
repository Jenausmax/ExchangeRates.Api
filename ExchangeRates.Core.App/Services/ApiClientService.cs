using ExchangeRates.Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRates.Core.App.Services
{
    public class ApiClientService : IApiClient
    {
        public HttpClient Client { get; set; }

        public ApiClientService()
        {
            Client = new HttpClient();  
        }
    }
}
