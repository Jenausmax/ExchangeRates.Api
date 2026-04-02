using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class StatisticsHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IMessageValute _valuteService;
        private readonly IUserService _userService;
        private readonly IBotService _botService;
        private readonly IKriptoApiClient _kriptoClient;

        public StatisticsHandler(
            IUpdateService updateService,
            IMessageValute valuteService,
            IUserService userService,
            IBotService botService,
            IKriptoApiClient kriptoClient)
        {
            _updateService = updateService;
            _valuteService = valuteService;
            _userService = userService;
            _botService = botService;
            _kriptoClient = kriptoClient;
        }

        /// <summary>
        /// Кнопка «Статистика» / команда /statistics → inline-меню выбора раздела
        /// </summary>
        public async Task HandleStatisticsCommand(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.StatsMenuHeader,
                new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(BotPhrases.BtnStatsValute, "stats_valute"),
                        InlineKeyboardButton.WithCallbackData(BotPhrases.BtnStatsCrypto, "stats_crypto")
                    }
                }));
        }

        /// <summary>
        /// Callback stats_valute → клавиатура периодов валют
        /// </summary>
        public async Task HandleStatsValute(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                "📊 Выберите период для статистики валют:",
                new InlineKeyboardMarkup(ValutePeriodKeyboard()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback stats_crypto → клавиатура периодов крипто
        /// </summary>
        public async Task HandleStatsCrypto(Update update)
        {
            await _updateService.EchoTextMessageAsync(
                update,
                "📊 Выберите период для статистики монет:",
                new InlineKeyboardMarkup(CryptoPeriodKeyboard()));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback period_{N} → статистика валют за N дней
        /// </summary>
        public async Task HandlePeriodCallback(Update update, int days)
        {
            var currencies = _userService.GetUserCurrencies(_userService.CurrentUser.ChatId);
            var message = await _valuteService.GetValuteStatisticsMessage(days, currencies, CancellationToken.None);

            if (string.IsNullOrEmpty(message))
            {
                await _updateService.EchoTextMessageAsync(
                    update,
                    "⚠️ Нет данных за указанный период. Попробуйте выбрать меньший период (3-7 дней).",
                    default);
            }
            else
            {
                await _updateService.EchoTextMessageAsync(update, message, default);
            }

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        /// <summary>
        /// Callback crypto_period_{N} → статистика монет за N дней
        /// </summary>
        public async Task HandleCryptoPeriodCallback(Update update, int days)
        {
            var coins = _userService.GetUserCryptoCoins(_userService.CurrentUser.ChatId);
            var symbols = coins ?? BotPhrases.AvailableCryptoCoins;
            var hours = days * 24;

            var sb = new StringBuilder();
            sb.AppendLine($"📊 *Статистика криптовалют за {FormatPeriod(days)}* (RUB)");
            sb.AppendLine();

            var hasData = false;
            var index = 1;

            foreach (var symbol in symbols)
            {
                var history = await _kriptoClient.GetHistoryAsync(symbol, "RUB", hours, CancellationToken.None);
                if (history?.Points == null || history.Points.Count < 2)
                    continue;

                // Группировка по дням — берём последнюю запись за каждый день
                var daily = history.Points
                    .GroupBy(p => p.FetchedAt.Date)
                    .Select(g => g.Last())
                    .OrderBy(p => p.FetchedAt)
                    .ToList();

                if (daily.Count < 2)
                    continue;

                hasData = true;
                var first = daily.First();
                var last = daily.Last();
                var min = daily.MinBy(p => p.Price);
                var max = daily.MaxBy(p => p.Price);
                var change = last.Price - first.Price;
                var changePct = first.Price != 0 ? (change / first.Price) * 100 : 0;
                var trend = change > 0 ? "↑" : change < 0 ? "↓" : "→";
                var sign = change >= 0 ? "+" : "";
                var name = BotPhrases.CryptoNames.GetValueOrDefault(symbol, symbol);

                sb.AppendLine($"{index}. *{symbol}* ({name})");
                sb.AppendLine($"   💰 Текущий: {FormatPrice(last.Price)} ₽");
                sb.AppendLine($"   ↑ Макс: {FormatPrice(max.Price)} ₽ ({max.FetchedAt:dd MMM})");
                sb.AppendLine($"   ↓ Мин: {FormatPrice(min.Price)} ₽ ({min.FetchedAt:dd MMM})");
                sb.AppendLine($"   {trend} Изм: {sign}{FormatPrice(Math.Abs(change))} ₽ ({sign}{changePct.ToString("F1", CultureInfo.InvariantCulture)}%)");
                sb.AppendLine();
                index++;
            }

            if (!hasData)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoStatsEmpty, default);
            }
            else
            {
                await _updateService.EchoTextMessageAsync(update, sb.ToString(), default);
            }

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private static List<List<InlineKeyboardButton>> ValutePeriodKeyboard()
        {
            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("3 дня", "period_3"),
                    InlineKeyboardButton.WithCallbackData("7 дней", "period_7"),
                    InlineKeyboardButton.WithCallbackData("14 дней", "period_14")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("21 день", "period_21"),
                    InlineKeyboardButton.WithCallbackData("30 дней", "period_30")
                }
            };
        }

        private static List<List<InlineKeyboardButton>> CryptoPeriodKeyboard()
        {
            return new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("3 дня", "crypto_period_3"),
                    InlineKeyboardButton.WithCallbackData("7 дней", "crypto_period_7"),
                    InlineKeyboardButton.WithCallbackData("14 дней", "crypto_period_14")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("21 день", "crypto_period_21"),
                    InlineKeyboardButton.WithCallbackData("30 дней", "crypto_period_30")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("2 мес", "crypto_period_60"),
                    InlineKeyboardButton.WithCallbackData("3 мес", "crypto_period_90"),
                    InlineKeyboardButton.WithCallbackData("4 мес", "crypto_period_120")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("5 мес", "crypto_period_150"),
                    InlineKeyboardButton.WithCallbackData("6 мес", "crypto_period_180")
                }
            };
        }

        private static string FormatPeriod(int days)
        {
            if (days <= 30) return $"{days} дней";
            var months = days / 30;
            return $"{months} мес";
        }

        private static string FormatPrice(decimal price)
        {
            if (price >= 1000)
                return price.ToString("N0", CultureInfo.InvariantCulture);
            if (price >= 1)
                return price.ToString("N2", CultureInfo.InvariantCulture);
            return price.ToString("N4", CultureInfo.InvariantCulture);
        }
    }
}
