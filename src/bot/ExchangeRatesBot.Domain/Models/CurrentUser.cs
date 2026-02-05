namespace ExchangeRatesBot.Domain.Models
{
    public class CurrentUser
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string NickName { get; set; }
    }
}