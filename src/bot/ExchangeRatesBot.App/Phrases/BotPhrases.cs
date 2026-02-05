namespace ExchangeRatesBot.App.Phrases
{
    public static class BotPhrases
    {
        public static string StartMenu { get; } = "Доброго времени суток! *Подписка* - получать курсы валют ЦБ РФ на USD, EUR, CNY, GBP, JPY за последние 7 дней.";
        public static string SubscribeTrue { get; } = "*Подписка оформлена!* Вы будете получать сообщения 2 раза в сутки. Спасибо!";
        public static string SubscribeFalse { get; } = "*Подписка отменена!* Мне очень жаль что вы отписались :((.";
        public static string Error { get; } = "Не правильный запрос. Попробуйте воспользоваться меню снизу.";
        public static string[] Valutes { get; } = new string[] { "USD", "EUR", "GBP", "JPY", "CNY" };
    }
}
