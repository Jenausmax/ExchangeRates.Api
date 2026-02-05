using ExchangeRatesBot.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IUserService
    {
        CurrentUser CurrentUser { get; set; }

        /// <summary>
        /// Метод установки CurrentUser'а.
        /// </summary>
        /// <param name="chatId">Чат id юзера.</param>
        /// <param name="user"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<bool> SetUser(long chatId, User user = default, CancellationToken cancel = default);
        Task<bool> Create(User user, CancellationToken cancel);
        Task<bool> SubscribeUpdate(long chatId, bool subscribe, CancellationToken cancel);
    }
}
