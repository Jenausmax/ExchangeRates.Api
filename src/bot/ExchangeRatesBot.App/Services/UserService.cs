using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.DB.Models;
using ExchangeRatesBot.Domain.Interfaces;
using ExchangeRatesBot.Domain.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.App.Services
{
    public class UserService : IUserService
    {
        private readonly IBaseRepositoryDb<UserDb> _userDb;


        public CurrentUser CurrentUser { get; set; }

        public UserService(IBaseRepositoryDb<UserDb> userDb)
        {
            _userDb = userDb;
            CurrentUser = new CurrentUser();
        }
        public async Task<bool> SetUser(long chatId, User user = default, CancellationToken cancel = default)
        {
            var users = await _userDb.GetCollection(cancel);
            if (chatId == 0) throw new NullReferenceException("User chatId null");

            var userGetCollection = users.FirstOrDefault(u => u.ChatId == chatId);
            if (userGetCollection is not null)
            {
                CurrentUser.Id = userGetCollection.Id;
                CurrentUser.ChatId = userGetCollection.ChatId;
                CurrentUser.NickName = userGetCollection.NickName;
                CurrentUser.Currencies = userGetCollection.Currencies;
                CurrentUser.NewsTimes = userGetCollection.NewsTimes;
                await _userDb.Update(userGetCollection, cancel);
                return true;
            }

            if (user is not null)
            {
                return await Create(user, cancel);
            }

            return false;
        }

        public async Task<bool> Create(User user, CancellationToken cancel)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            var userDb = new UserDb();
            userDb.ChatId = user.ChatId;
            userDb.FirstName = user.FirstName;
            userDb.LastName = user.LastName;
            userDb.NickName = user.NickName;
            userDb.Subscribe = user.Subscribe;
            userDb.Currencies = user.Currencies;  // NEW: будет null для новых пользователей

            return await _userDb.Create(userDb, cancel);
        }

        public async Task<bool> SubscribeUpdate(long chatId, bool subscribe, CancellationToken cancel)
        {
            var usersDb = await _userDb.GetCollection(cancel);

            var userDb = usersDb.FirstOrDefault(u => u.ChatId == chatId);
            if (userDb == null)
            {
                return false;
            }

            userDb.Subscribe = subscribe;
            await _userDb.Update(userDb, cancel);
            return true;
        }

        /// <summary>
        /// Обновить выбранные валюты пользователя
        /// </summary>
        public async Task<bool> UpdateCurrencies(long chatId, string currencies, CancellationToken cancel)
        {
            var usersDb = await _userDb.GetCollection(cancel);
            var userDb = usersDb.FirstOrDefault(u => u.ChatId == chatId);
            if (userDb == null)
            {
                return false;
            }

            userDb.Currencies = currencies;
            await _userDb.Update(userDb, cancel);
            return true;
        }

        /// <summary>
        /// Получить выбранные валюты пользователя (синхронный, работает с CurrentUser)
        /// </summary>
        public string[] GetUserCurrencies(long chatId)
        {
            if (CurrentUser != null && CurrentUser.ChatId == chatId && CurrentUser.Currencies != null)
            {
                return CurrentUser.Currencies.Split(',');
            }
            return BotPhrases.Valutes;  // дефолтный набор
        }

        /// <summary>
        /// Обновить подписку пользователя на новостной дайджест
        /// </summary>
        public async Task<bool> NewsSubscribeUpdate(long chatId, bool subscribe, CancellationToken cancel)
        {
            var usersDb = await _userDb.GetCollection(cancel);
            var userDb = usersDb.FirstOrDefault(u => u.ChatId == chatId);
            if (userDb == null)
            {
                return false;
            }

            userDb.NewsSubscribe = subscribe;
            await _userDb.Update(userDb, cancel);
            return true;
        }

        /// <summary>
        /// Обновить персональное расписание новостей пользователя
        /// </summary>
        public async Task<bool> UpdateNewsTimes(long chatId, string newsTimes, CancellationToken cancel)
        {
            var usersDb = await _userDb.GetCollection(cancel);
            var userDb = usersDb.FirstOrDefault(u => u.ChatId == chatId);
            if (userDb == null)
            {
                return false;
            }

            userDb.NewsTimes = newsTimes;
            await _userDb.Update(userDb, cancel);
            return true;
        }

        /// <summary>
        /// Обновить время последней доставки новостей пользователю
        /// </summary>
        public async Task<bool> UpdateLastNewsDeliveredAt(long chatId, DateTime deliveredAt, CancellationToken cancel)
        {
            var usersDb = await _userDb.GetCollection(cancel);
            var userDb = usersDb.FirstOrDefault(u => u.ChatId == chatId);
            if (userDb == null)
            {
                return false;
            }

            userDb.LastNewsDeliveredAt = deliveredAt;
            await _userDb.Update(userDb, cancel);
            return true;
        }

        /// <summary>
        /// Получить персональное расписание новостей пользователя (синхронный, работает с CurrentUser)
        /// </summary>
        public string[] GetUserNewsTimes(long chatId)
        {
            if (CurrentUser != null && CurrentUser.ChatId == chatId && CurrentUser.NewsTimes != null)
            {
                return CurrentUser.NewsTimes.Split(',');
            }
            return Array.Empty<string>();
        }
    }
}
