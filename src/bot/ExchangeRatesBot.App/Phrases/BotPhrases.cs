namespace ExchangeRatesBot.App.Phrases
{
    public static class BotPhrases
    {
        // --- Существующие фразы (без изменений) ---
        public static string StartMenu { get; } = "Доброго времени суток! *Подписка* - получать курсы валют ЦБ РФ на USD, EUR, CNY, GBP, JPY за последние 7 дней.";
        public static string SubscribeTrue { get; } = "*Подписка оформлена!* Вы будете получать сообщения 2 раза в сутки. Спасибо!";
        public static string SubscribeFalse { get; } = "*Подписка отменена!* Мне очень жаль что вы отписались :((.";
        public static string Error { get; } = "Не правильный запрос. Попробуйте воспользоваться меню снизу.";
        public static string[] Valutes { get; } = new string[] { "USD", "EUR", "GBP", "JPY", "CNY" };

        // --- Новые константы для ReplyKeyboard ---

        /// <summary>
        /// Тексты кнопок ReplyKeyboard. Используются и для создания клавиатуры,
        /// и для маппинга входящего текста на команды в CommandService.
        /// </summary>
        public static string BtnValuteOneDay { get; } = "Курс сегодня";
        public static string BtnValuteSevenDays { get; } = "За 7 дней";
        public static string BtnSubscribe { get; } = "Подписка";
        public static string BtnHelp { get; } = "Помощь";

        /// <summary>
        /// Текст ответа на команду /help.
        /// </summary>
        public static string HelpMessage { get; } =
            "*Доступные команды:*\n\r" +
            "Курс сегодня -- курсы валют ЦБ РФ на сегодня\n\r" +
            "За 7 дней -- изменения курсов за последние 7 дней\n\r" +
            "Подписка -- подписаться/отписаться от рассылки курсов\n\r" +
            "Помощь -- это сообщение\n\r\n\r" +
            "_Также доступны команды:_ /valuteoneday, /valutesevendays, /subscribe, /help";
    }
}
