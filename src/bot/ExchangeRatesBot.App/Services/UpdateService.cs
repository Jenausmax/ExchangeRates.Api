using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeRatesBot.Domain.Interfaces;
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

        public async Task EchoTextMessageAsync(Update update, string message, InlineKeyboardMarkup keyboard = default)
        {
            if (update == null) return;

            if (update.Type == UpdateType.Message) //обработка текстовых сообщений
            {
                if (update.Message != null)
                {
                    var newMessage = update.Message;
                    newMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(newMessage.Chat.Id,
                        newMessage.Text,
                        parseMode: ParseMode.Markdown,
                        entities: null,
                        disableWebPagePreview: false,
                        disableNotification: false,
                        replyToMessageId: 0,
                        allowSendingWithoutReply: false,
                        keyboard);
                }
            }

            if (update.Type == UpdateType.CallbackQuery) //обработка калбеков
            {
                if (update.CallbackQuery.Message != null)
                {
                    var newMessageCallbackQueryMessage = update.CallbackQuery.Message;
                    newMessageCallbackQueryMessage.Text = message;
                    await _botService.Client.SendTextMessageAsync(newMessageCallbackQueryMessage.Chat.Id,
                        newMessageCallbackQueryMessage.Text,
                        parseMode: ParseMode.Markdown,
                        entities: null,
                        disableWebPagePreview: false,
                        disableNotification: false,
                        replyToMessageId: 0,
                        allowSendingWithoutReply: false,
                        keyboard);
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
