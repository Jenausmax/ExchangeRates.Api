using ExchangeRatesBot.Domain.Models;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Обновить выбранные валюты пользователя
        /// </summary>
        Task<bool> UpdateCurrencies(long chatId, string currencies, CancellationToken cancel);

        /// <summary>
        /// Получить выбранные валюты пользователя (синхронный, работает с CurrentUser)
        /// </summary>
        string[] GetUserCurrencies(long chatId);

        /// <summary>
        /// Обновить подписку пользователя на новостной дайджест
        /// </summary>
        Task<bool> NewsSubscribeUpdate(long chatId, bool subscribe, CancellationToken cancel);

        /// <summary>
        /// Обновить персональное расписание новостей пользователя
        /// </summary>
        Task<bool> UpdateNewsTimes(long chatId, string newsTimes, CancellationToken cancel);

        /// <summary>
        /// Обновить время последней доставки новостей пользователю
        /// </summary>
        Task<bool> UpdateLastNewsDeliveredAt(long chatId, DateTime deliveredAt, CancellationToken cancel);

        /// <summary>
        /// Получить персональное расписание новостей пользователя (синхронный, работает с CurrentUser)
        /// </summary>
        string[] GetUserNewsTimes(long chatId);

        /// <summary>
        /// Обновить подписку пользователя на важные новости
        /// </summary>
        Task<bool> ImportantNewsSubscribeUpdate(long chatId, bool subscribe, CancellationToken cancel);

        /// <summary>
        /// Обновить время последней доставки важных новостей пользователю
        /// </summary>
        Task<bool> UpdateLastImportantNewsAt(long chatId, DateTime deliveredAt, CancellationToken cancel);
    }
}
