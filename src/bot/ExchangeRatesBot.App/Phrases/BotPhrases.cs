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

        // --- Новые константы для выбора валют ---

        /// <summary>
        /// Список валют для выбора (топ-10 популярных)
        /// </summary>
        public static string[] AvailableCurrencies { get; } = new string[]
            { "USD", "EUR", "GBP", "JPY", "CNY", "CHF", "CAD", "AUD", "TRY", "BYN" };

        /// <summary>
        /// Дефолтный набор (текущее поведение)
        /// </summary>
        public static string DefaultCurrencies { get; } = "USD,EUR,GBP,JPY,CNY";

        /// <summary>
        /// Заголовок для клавиатуры выбора валют
        /// </summary>
        public static string CurrenciesHeader { get; } = "Выберите валюты для отслеживания:";

        /// <summary>
        /// Сообщение об успешном сохранении валют
        /// </summary>
        public static string CurrenciesSaved { get; } = "*Настройки сохранены!* Выбранные валюты: ";

        /// <summary>
        /// Предупреждение при попытке сохранить пустой набор
        /// </summary>
        public static string CurrenciesEmpty { get; } = "Выберите хотя бы одну валюту.";

        // --- Новые константы для ReplyKeyboard ---

        /// <summary>
        /// Тексты кнопок ReplyKeyboard. Используются и для создания клавиатуры,
        /// и для маппинга входящего текста на команды в CommandService.
        /// </summary>
        public static string BtnValuteOneDay { get; } = "Курс сегодня";
        public static string BtnValuteSevenDays { get; } = "За 7 дней";
        public static string BtnStatistics { get; } = "Статистика";
        public static string BtnSubscribe { get; } = "Подписка";
        public static string BtnHelp { get; } = "Помощь";
        public static string BtnCurrencies { get; } = "Валюты";

        /// <summary>
        /// Текст ответа на команду /help.
        /// </summary>
        public static string HelpMessage { get; } =
            "*Доступные команды:*\n\r" +
            "Курс сегодня -- курсы валют ЦБ РФ на сегодня\n\r" +
            "За 7 дней -- изменения курсов за последние 7 дней\n\r" +
            "Статистика -- детальная статистика за период (3-30 дней)\n\r" +
            "Валюты -- выбор валют для отслеживания\n\r" +
            "Подписка -- подписаться/отписаться от рассылки курсов\n\r" +
            "Новости -- новостной дайджест и подписка на новости\n\r" +
            "Помощь -- это сообщение\n\r\n\r" +
            "_Также доступны команды:_ /valuteoneday, /valutesevendays, /statistics, /currencies, /subscribe, /news, /help";

        // --- Новостной дайджест ---

        /// <summary>
        /// Текст кнопки "Новости" в ReplyKeyboard
        /// </summary>
        public static string BtnNews { get; } = "Новости";

        /// <summary>
        /// Подтверждение подписки на новостной дайджест
        /// </summary>
        public static string NewsSubscribeTrue { get; } = "*Подписка на новости оформлена!* Вы будете получать новостной дайджест.";

        /// <summary>
        /// Подтверждение отписки от новостного дайджеста
        /// </summary>
        public static string NewsSubscribeFalse { get; } = "*Подписка на новости отменена.*";

        /// <summary>
        /// Сообщение при отсутствии новых новостей
        /// </summary>
        public static string NewsEmpty { get; } = "Новых новостей пока нет. Попробуйте позже.";

        /// <summary>
        /// Заголовок меню новостей
        /// </summary>
        public static string NewsHeader { get; } = "Выберите действие:";

        // --- Персонализированное расписание новостей ---

        public static string NewsScheduleHeader { get; } = "Выберите время для получения новостей:\n(активные слоты отмечены ✅)";

        public static string NewsScheduleSaved { get; } = "*Расписание сохранено!* Вы будете получать новости в: ";

        public static string NewsScheduleEmpty { get; } = "Выберите хотя бы одно время для получения новостей.";

        public static string NewsSubscribeTrueSchedule { get; } = "*Подписка на новости оформлена!* Расписание: 09:00. Настроить расписание можно через кнопку «Настроить расписание».";

        public static string NewsAlreadySubscribed { get; } = "Вы уже подписаны на новости. Настроить расписание можно через кнопку «Настроить расписание».";

        /// <summary>
        /// Доступные слоты расписания (каждые 3 часа)
        /// </summary>
        public static string[] AvailableNewsSlots { get; } = new string[]
            { "06:00", "09:00", "12:00", "15:00", "18:00", "21:00" };
    }
}
