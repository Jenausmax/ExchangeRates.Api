using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class CryptoHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly IKriptoApiClient _kriptoClient;
        private readonly IUserSelectionState _state;

        public CryptoHandler(IUpdateService updateService, IBotService botService, IUserService userService, IKriptoApiClient kriptoClient, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _kriptoClient = kriptoClient;
            _state = state;
        }

        public async Task HandleCryptoCommand(Update update, string currency)
        {
            var coins = _userService.GetUserCryptoCoins(_userService.CurrentUser.ChatId);
            var symbols = coins != null ? string.Join(",", coins) : null;
            var result = await _kriptoClient.GetLatestPricesAsync(currency, symbols, CancellationToken.None);

            if (result.Prices == null || result.Prices.Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoEmpty, default);
                return;
            }

            var message = FormatCryptoPrices(result, currency);
            var keyboard = CryptoInlineKeyboard(currency);
            await _updateService.EchoTextMessageAsync(update, message, keyboard);
        }

        public async Task HandleCryptoCallback(Update update, string currency)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;
            var coins = _userService.GetUserCryptoCoins(update.CallbackQuery.From.Id);
            var symbols = coins != null ? string.Join(",", coins) : null;
            var result = await _kriptoClient.GetLatestPricesAsync(currency, symbols, CancellationToken.None);

            if (result.Prices == null || result.Prices.Count == 0)
            {
                await _botService.Client.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: BotPhrases.CryptoEmpty);
                return;
            }

            var message = FormatCryptoPrices(result, currency);
            var keyboard = CryptoInlineKeyboard(currency);

            await _botService.Client.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: message,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleCryptoCoinsCommand(Update update)
        {
            var chatId = _userService.CurrentUser.ChatId;
            var currentCoins = _userService.GetUserCryptoCoins(chatId);
            _state.PendingCryptoCoins[chatId] = currentCoins != null
                ? new HashSet<string>(currentCoins)
                : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CryptoCoinsHeader,
                new InlineKeyboardMarkup(CryptoCoinsKeyboard(chatId)));
        }

        public async Task HandleToggleCryptoSymbol(Update update, string symbol)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCryptoCoins.ContainsKey(chatId))
            {
                var currentCoins = _userService.GetUserCryptoCoins(chatId);
                _state.PendingCryptoCoins[chatId] = currentCoins != null
                    ? new HashSet<string>(currentCoins)
                    : new HashSet<string>(BotPhrases.AvailableCryptoCoins);
            }

            var selection = _state.PendingCryptoCoins[chatId];
            if (selection.Contains(symbol))
                selection.Remove(symbol);
            else
                selection.Add(symbol);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(CryptoCoinsKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveCryptoCoins(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCryptoCoins.ContainsKey(chatId) || _state.PendingCryptoCoins[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CryptoCoinsEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _state.PendingCryptoCoins[chatId];
            var coinsString = string.Join(",", selected);
            await _userService.UpdateCryptoCoins(chatId, coinsString, CancellationToken.None);

            _state.PendingCryptoCoins.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CryptoCoinsSaved + coinsString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> CryptoCoinsKeyboard(long chatId)
        {
            var selected = _state.PendingCryptoCoins.ContainsKey(chatId)
                ? _state.PendingCryptoCoins[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var coin in BotPhrases.AvailableCryptoCoins)
            {
                var isSelected = selected.Contains(coin);
                var label = isSelected ? $"\u2705 {coin}" : $"\u2B1C {coin}";
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_crypto_{coin}"));

                if (currentRow.Count == 3)
                {
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);

            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("\u2705 Сохранить", "save_crypto_coins")
            });

            return rows;
        }

        private static string FormatCryptoPrices(CryptoPriceResult result, string currency)
        {
            var currencySign = currency == "RUB" ? "\u20BD" : "$";
            var sb = new StringBuilder();
            sb.AppendLine($"*Курсы криптовалют ({EscapeMarkdown(currency)})*");
            sb.AppendLine($"_{result.FetchedAt:dd.MM.yyyy HH:mm} UTC_");
            sb.AppendLine();

            var index = 1;
            foreach (var item in result.Prices)
            {
                var arrow = item.ChangePct24h >= 0 ? "\U0001F7E2" : "\U0001F534";
                var sign = item.ChangePct24h >= 0 ? "+" : "";
                var name = EscapeMarkdown(BotPhrases.CryptoNames.GetValueOrDefault(item.Symbol ?? "", item.Symbol ?? ""));
                var symbol = EscapeMarkdown(item.Symbol ?? "");
                var priceStr = FormatCryptoPrice(item.Price);

                sb.AppendLine($"{index}. *{symbol}* ({name})");
                sb.AppendLine($"   {priceStr} {currencySign}  {arrow} {sign}{item.ChangePct24h.ToString("F1", CultureInfo.InvariantCulture)}%");
                sb.AppendLine();
                index++;
            }

            return sb.ToString();
        }

        private static string FormatCryptoPrice(decimal price)
        {
            if (price >= 1000)
                return price.ToString("N0", CultureInfo.InvariantCulture);
            if (price >= 1)
                return price.ToString("N2", CultureInfo.InvariantCulture);
            return price.ToString("N4", CultureInfo.InvariantCulture);
        }

        private static string EscapeMarkdown(string text)
            => text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("`", "\\`");

        private static InlineKeyboardMarkup CryptoInlineKeyboard(string activeCurrency)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        activeCurrency == "RUB" ? "[ RUB ]" : "RUB", "crypto_rub"),
                    InlineKeyboardButton.WithCallbackData(
                        activeCurrency == "USD" ? "[ USD ]" : "USD", "crypto_usd")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        "\U0001F504 Обновить", $"crypto_refresh_{activeCurrency.ToLower()}")
                }
            });
        }
    }
}
