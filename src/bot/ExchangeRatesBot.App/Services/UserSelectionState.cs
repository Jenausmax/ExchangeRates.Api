using System.Collections.Concurrent;
using System.Collections.Generic;
using ExchangeRatesBot.Domain.Interfaces;

namespace ExchangeRatesBot.App.Services
{
    public class UserSelectionState : IUserSelectionState
    {
        public ConcurrentDictionary<long, HashSet<string>> PendingCurrencies { get; } = new();
        public ConcurrentDictionary<long, HashSet<string>> PendingNewsSchedule { get; } = new();
        public ConcurrentDictionary<long, HashSet<string>> PendingCryptoCoins { get; } = new();
    }
}
