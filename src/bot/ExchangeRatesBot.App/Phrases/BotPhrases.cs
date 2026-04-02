namespace ExchangeRatesBot.App.Phrases
{
    public static class BotPhrases
    {
        // --- Существующие фразы (без изменений) ---
        public static string StartMenu { get; } = "Доброго времени суток! Используйте кнопки меню для навигации.";
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

        // --- BOT-0027: Зонтичные reply-кнопки ---
        public static string BtnRates { get; } = "Курсы";
        public static string BtnSettings { get; } = "Настройки";

        // --- BOT-0027: Inline-кнопки в меню «Курсы» ---
        public static string BtnRatesValute { get; } = "Курс валют";
        public static string BtnRatesCrypto { get; } = "Курс монет";

        // --- BOT-0027: Inline-кнопки в меню «Настройки» ---
        public static string BtnSettingsCurrencies { get; } = "Настройки валют";
        public static string BtnSettingsCryptoCoins { get; } = "Настройки монет";
        public static string BtnSettingsSubscribe { get; } = "Подписка";

        // --- BOT-0027: Заголовки inline-меню ---
        public static string RatesMenuHeader { get; } = "Выберите раздел:";
        public static string SettingsMenuHeader { get; } = "Настройки:";

        /// <summary>
        /// Текст ответа на команду /help.
        /// </summary>
        public static string HelpMessage { get; } =
            "*Доступные команды:*\n\r" +
            "Курсы -- курсы валют и криптовалют\n\r" +
            "Новости -- лента последних новостей\n\r" +
            "Статистика -- детальная статистика за период (3-30 дней)\n\r" +
            "Настройки -- настройки валют, монет и подписок\n\r" +
            "Помощь -- это сообщение\n\r\n\r" +
            "_Также доступны команды:_ /valuteoneday, /valutesevendays, /statistics, /currencies, /subscribe, /news, /crypto, /cryptocoins, /help";

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

        // --- Пагинация новостей ---

        public static string NewsNoMore { get; } = "Больше новостей нет.";
        public static string BtnNewsMore { get; } = "Далее \u2B07\uFE0F";

        // --- Важные новости ---

        public static string ImportantNewsSubscribeTrue { get; } = "*Подписка на важные новости оформлена!* Вы будете получать самую обсуждаемую новость каждый час.";
        public static string ImportantNewsSubscribeFalse { get; } = "*Подписка на важные новости отменена.*";
        public static string ImportantNewsAlreadySubscribed { get; } = "Вы уже подписаны на важные новости.";

        // --- Меню подписок ---

        public static string SubscriptionMenuHeader { get; } = "Управление подписками:";
        public static string NewsDigestMenuHeader { get; } = "Новостной дайджест:";

        // --- Криптовалюты ---

        /// <summary>
        /// Текст кнопки "Крипто" в ReplyKeyboard
        /// </summary>
        public static string BtnCrypto { get; } = "Крипто";

        /// <summary>
        /// Сообщение при недоступности данных о криптовалютах
        /// </summary>
        public static string CryptoEmpty { get; } = "Данные о криптовалютах временно недоступны. Попробуйте позже.";

        /// <summary>
        /// Текст кнопки "Монеты" в ReplyKeyboard
        /// </summary>
        public static string BtnCryptoCoins { get; } = "Монеты";

        public static string CryptoCoinsHeader { get; } = "Выберите криптовалюты для отслеживания:";
        public static string CryptoCoinsSaved { get; } = "*Настройки сохранены!* Выбранные монеты: ";
        public static string CryptoCoinsEmpty { get; } = "Выберите хотя бы одну криптовалюту.";

        public static string[] AvailableCryptoCoins { get; } = new string[]
            { "BTC", "ETH", "SOL", "XRP", "BNB", "USDT", "DOGE", "ADA", "TON", "AVAX" };

        /// <summary>
        /// Человекочитаемые названия криптовалют
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, string> CryptoNames { get; } = new()
        {
            ["BTC"] = "Bitcoin",
            ["ETH"] = "Ethereum",
            ["SOL"] = "Solana",
            ["XRP"] = "XRP",
            ["BNB"] = "BNB",
            ["USDT"] = "Tether",
            ["DOGE"] = "Dogecoin",
            ["ADA"] = "Cardano",
            ["TON"] = "Toncoin",
            ["AVAX"] = "Avalanche"
        };
    }
}
