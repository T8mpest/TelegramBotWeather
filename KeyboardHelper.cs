using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotWeather
{
    public static class KeyboardHelper
    {
        public static ReplyKeyboardMarkup GetCommandsKeyboard()
        {
            var keyboardButtons = new List<KeyboardButton[]>
            {
                new[] { new KeyboardButton("/temperature") },
                // Добавьте другие кнопки команд по аналогии
            };

            return new ReplyKeyboardMarkup(keyboardButtons);
        }
    }
}