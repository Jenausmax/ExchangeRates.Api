namespace ExchangeRatesBot.Domain.Models
{
    public class CurrentUser
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string NickName { get; set; }
        public bool Subscribe { get; set; }
        public string Currencies { get; set; }
        public bool NewsSubscribe { get; set; }
        public string NewsTimes { get; set; }
        public bool ImportantNewsSubscribe { get; set; }
    }
}