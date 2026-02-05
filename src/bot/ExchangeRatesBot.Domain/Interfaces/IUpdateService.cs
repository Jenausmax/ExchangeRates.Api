using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Метод отправки сообщения в чат от бота.
        /// </summary>
        /// <param name="update">Пришедший апдейт</param>
        /// <param name="message">Новое сообщение.</param>
        /// <param name="keyboard">Клавиатура для взаимодействия.</param>
        /// <returns></returns>
        Task EchoTextMessageAsync(Update update, string message, InlineKeyboardMarkup keyboard = default);
    }
}
