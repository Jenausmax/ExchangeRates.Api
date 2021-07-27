using System.Net.Http;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IApiClient
    {
        HttpClient Client { get; set; }

    }
}
