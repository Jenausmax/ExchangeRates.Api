using System.Net.Http;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IApiClient
    {
        HttpClient Client { get; set; }
    }
}
