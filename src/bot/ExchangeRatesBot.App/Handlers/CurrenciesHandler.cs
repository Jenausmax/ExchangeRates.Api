using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRatesBot.App.Phrases;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Handlers
{
    public class CurrenciesHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly IUserSelectionState _state;

        public CurrenciesHandler(IUpdateService updateService, IBotService botService, IUserService userService, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _state = state;
        }

        public async Task HandleCurrenciesCommand(Update update)
        {
            var chatId = _userService.CurrentUser.ChatId;
            var currentCurrencies = _userService.GetUserCurrencies(chatId);
            _state.PendingCurrencies[chatId] = new HashSet<string>(currentCurrencies);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CurrenciesHeader,
                new InlineKeyboardMarkup(CurrenciesKeyboard(chatId)));
        }

        public async Task HandleToggleCurrency(Update update, string currencyCode)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCurrencies.ContainsKey(chatId))
            {
                var currentCurrencies = _userService.GetUserCurrencies(chatId);
                _state.PendingCurrencies[chatId] = new HashSet<string>(currentCurrencies);
            }

            var selection = _state.PendingCurrencies[chatId];
            if (selection.Contains(currencyCode))
                selection.Remove(currencyCode);
            else
                selection.Add(currencyCode);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(CurrenciesKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveCurrencies(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingCurrencies.ContainsKey(chatId) || _state.PendingCurrencies[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.CurrenciesEmpty, default);
                return;
            }

            var selected = _state.PendingCurrencies[chatId];
            var currenciesString = string.Join(",", selected);
            await _userService.UpdateCurrencies(chatId, currenciesString, CancellationToken.None);

            _state.PendingCurrencies.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.CurrenciesSaved + currenciesString,
                default);
        }

        private List<List<InlineKeyboardButton>> CurrenciesKeyboard(long chatId)
        {
            var selected = _state.PendingCurrencies.ContainsKey(chatId)
                ? _state.PendingCurrencies[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var currency in BotPhrases.AvailableCurrencies)
            {
                var isSelected = selected.Contains(currency);
                var label = isSelected ? $"✅ {currency}" : $"⬜ {currency}";
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_{currency}"));

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
                InlineKeyboardButton.WithCallbackData("✅ Сохранить", "save_currencies")
            });

            return rows;
        }
    }
}
