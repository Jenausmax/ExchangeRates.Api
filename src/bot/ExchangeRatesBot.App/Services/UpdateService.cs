using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeRatesBot.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.App.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly IBotService _botService;

        public UpdateService(IBotService botService)
        {
            _botService = botService;
        }

        public async Task EchoTextMessageAsync(Update update, string message, IReplyMarkup replyMarkup = null)
        {
            if (update == null) return;

            if (update.Type == UpdateType.Message) //обработка текстовых сообщений
            {
                if (update.Message != null)
                {
                    var newMessage = update.Message;
                    newMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(
                        chatId: newMessage.Chat.Id,
                        text: newMessage.Text,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: replyMarkup);
                }
            }

            if (update.Type == UpdateType.CallbackQuery) //обработка калбеков
            {
                if (update.CallbackQuery.Message != null)
                {
                    var newMessageCallbackQueryMessage = update.CallbackQuery.Message;
                    newMessageCallbackQueryMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(
                        chatId: newMessageCallbackQueryMessage.Chat.Id,
                        text: newMessageCallbackQueryMessage.Text,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: replyMarkup);
                }
            }

            //TODO: описать обработку ботом сообщений из групповых чатов. 
            if (update.Type == UpdateType.ChannelPost)
            {
                return;
            }
        }
    }
}
