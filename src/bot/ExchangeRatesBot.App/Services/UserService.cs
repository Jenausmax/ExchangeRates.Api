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
    }
}
