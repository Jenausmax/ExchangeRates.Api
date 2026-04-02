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
    public class NewsHandler
    {
        private readonly IUpdateService _updateService;
        private readonly IBotService _botService;
        private readonly IUserService _userService;
        private readonly INewsApiClient _newsClient;
        private readonly IUserSelectionState _state;

        public NewsHandler(IUpdateService updateService, IBotService botService, IUserService userService, INewsApiClient newsClient, IUserSelectionState state)
        {
            _updateService = updateService;
            _botService = botService;
            _userService = userService;
            _newsClient = newsClient;
            _state = state;
        }

        public async Task HandleNewsCommand(Update update)
        {
            var digest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
        }

        public async Task HandleNewsLatest(Update update)
        {
            var digest = await _newsClient.GetLatestDigestAsync(5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsEmpty, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleNewsPage(Update update, int beforeId)
        {
            var digest = await _newsClient.GetDigestBeforeIdAsync(beforeId, 5, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(digest?.Message))
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsNoMore, default);
            }
            else
            {
                var replyMarkup = digest.HasMore && digest.TopicIds?.Count > 0
                    ? new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(BotPhrases.BtnNewsMore, $"news_p_{digest.TopicIds.Last()}")
                        }
                    })
                    : null;
                await _updateService.EchoTextMessageAsync(update, digest.Message, replyMarkup);
            }
        }

        public async Task HandleScheduleCommand(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;
            var currentTimes = _userService.GetUserNewsTimes(chatId);
            _state.PendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.NewsScheduleHeader,
                new InlineKeyboardMarkup(NewsScheduleKeyboard(chatId)));
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleToggleNewsSlot(Update update, string timeSlotKey)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingNewsSchedule.ContainsKey(chatId))
            {
                var currentTimes = _userService.GetUserNewsTimes(chatId);
                _state.PendingNewsSchedule[chatId] = new HashSet<string>(currentTimes);
            }

            var fullSlot = timeSlotKey + ":00";
            var selection = _state.PendingNewsSchedule[chatId];
            if (selection.Contains(fullSlot))
                selection.Remove(fullSlot);
            else
                selection.Add(fullSlot);

            await _botService.Client.EditMessageReplyMarkupAsync(
                chatId: update.CallbackQuery.Message.Chat.Id,
                messageId: update.CallbackQuery.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(NewsScheduleKeyboard(chatId)));

            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        public async Task HandleSaveNewsSchedule(Update update)
        {
            var chatId = update.CallbackQuery.From.Id;

            if (!_state.PendingNewsSchedule.ContainsKey(chatId) || _state.PendingNewsSchedule[chatId].Count == 0)
            {
                await _updateService.EchoTextMessageAsync(update, BotPhrases.NewsScheduleEmpty, default);
                await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                return;
            }

            var selected = _state.PendingNewsSchedule[chatId];
            var sortedSlots = selected.OrderBy(s => s).ToArray();
            var newsTimesString = string.Join(",", sortedSlots);
            await _userService.UpdateNewsTimes(chatId, newsTimesString, CancellationToken.None);
            await _userService.NewsSubscribeUpdate(chatId, true, CancellationToken.None);

            _state.PendingNewsSchedule.TryRemove(chatId, out _);

            await _updateService.EchoTextMessageAsync(
                update,
                BotPhrases.NewsScheduleSaved + newsTimesString,
                default);
            await _botService.Client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
        }

        private List<List<InlineKeyboardButton>> NewsScheduleKeyboard(long chatId)
        {
            var selected = _state.PendingNewsSchedule.ContainsKey(chatId)
                ? _state.PendingNewsSchedule[chatId]
                : new HashSet<string>();

            var rows = new List<List<InlineKeyboardButton>>();
            var currentRow = new List<InlineKeyboardButton>();

            foreach (var slot in BotPhrases.AvailableNewsSlots)
            {
                var isSelected = selected.Contains(slot);
                var label = isSelected ? $"✅ {slot}" : slot;
                var slotKey = slot.Substring(0, 2);
                currentRow.Add(InlineKeyboardButton.WithCallbackData(label, $"toggle_news_{slotKey}"));

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
                InlineKeyboardButton.WithCallbackData("Сохранить", "save_news_schedule")
            });

            return rows;
        }
    }
}
