using KriptoService.Domain.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Domain.Interfaces
{
    public interface ICryptoService
    {
        Task<CryptoPriceResponse> GetLatestAsync(string[] symbols, string[] currencies, CancellationToken cancel = default);
        Task<CryptoHistoryResponse> GetHistoryAsync(string symbol, string currency, int hours, CancellationToken cancel = default);
        Task<CryptoStatusResponse> GetStatusAsync(CancellationToken cancel = default);
    }
}
