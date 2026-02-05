using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IMessageValute
    {
        /// <summary>
        /// Метод формирования строки ответа.
        /// </summary>
        /// <param name="day">Количество дней.</param>
        /// <param name="charCode">Код валюты.</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<string> GetValuteMessage(int day, string charCode, CancellationToken cancel);

        /// <summary>
        /// Метод формирования строки ответа.
        /// </summary>
        /// <param name="day">Количество дней.</param>
        /// <param name="charCodesCollection">Массив строк кодов валют.</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<string> GetValuteMessage(int day, string[] charCodesCollection, CancellationToken cancel);
    }
}
