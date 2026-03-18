using ExchangeRatesBot.Domain.Models;
using System;

namespace ExchangeRatesBot.DB.Models
{
    public class UserDb : Entity
    {
        public long ChatId { get; set; }
        public string NickName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Subscribe { get; set; }
        public string Currencies { get; set; }
        public bool NewsSubscribe { get; set; }
        public string NewsTimes { get; set; }
        public DateTime? LastNewsDeliveredAt { get; set; }
    }
}
