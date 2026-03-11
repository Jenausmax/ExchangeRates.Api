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

        /// <summary>
        /// Формирует компактную сводку курсов с недельной динамикой для рассылки.
        /// </summary>
        Task<string> GetValuteSummaryMessage(string[] charCodesCollection, CancellationToken cancel);

        /// <summary>
        /// Формирует компактное сообщение со статистикой за указанный период.
        /// Использует разные форматы в зависимости от длины периода.
        /// </summary>
        /// <param name="days">Количество дней для анализа (3-30)</param>
        /// <param name="charCodesCollection">Массив кодов валют</param>
        /// <param name="cancel">Токен отмены</param>
        /// <returns>Отформатированное сообщение со статистикой</returns>
        Task<string> GetValuteStatisticsMessage(int days, string[] charCodesCollection, CancellationToken cancel);
    }
}
