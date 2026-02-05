using ExchangeRatesBot.Domain.Models;

namespace ExchangeRatesBot.DB.Models
{
    public class UserDb : Entity
    {
        public long ChatId { get; set; }
        public string NickName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Subscribe { get; set; }
    }
}
