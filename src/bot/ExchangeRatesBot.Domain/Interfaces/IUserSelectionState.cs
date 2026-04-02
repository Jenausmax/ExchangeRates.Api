using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IUserSelectionState
    {
        ConcurrentDictionary<long, HashSet<string>> PendingCurrencies { get; }
        ConcurrentDictionary<long, HashSet<string>> PendingNewsSchedule { get; }
        ConcurrentDictionary<long, HashSet<string>> PendingCryptoCoins { get; }
    }
}
