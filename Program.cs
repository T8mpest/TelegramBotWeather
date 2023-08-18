using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWeather;
using Telegram.Bot.Types.InlineQueryResults;
using System.Threading;
using System;
namespace TelegramBotWeather

{
    class Program
    {
        private const string BotToken = "6154663885:AAFfH8MfyJ263NGUvEFipcTkLD1Hx46a2rY";
        private const string WeatherApiKey = "https://api.openweathermap.org/data/2.5/weather?q=Dnipro&appid=ca085266e256c19f8ad8a74dbcfe86e2";

        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient(BotToken);

            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            var me = await botClient.GetMeAsync();
            botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cts.Cancel();
        }



        private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update?.Message?.Type != MessageType.Text)
                return;

            var message = update.Message;

            if (message.Text == "/start")
            {
               // await bot.SendStickerAsync(update.Message.Chat.Id, InputFile.FromString("https://imgur.com/TtZMndX"), allowSendingWithoutReply: true, cancellationToken: ct);
            }
            else if (message.Text.StartsWith("/temperature"))
            {
                var city = message.Text.Substring(12).Trim();
                var weatherInfo = await GetWeatherInfo(city);

                if (weatherInfo != null)
                {
                    var response = $"Температура в городе {city}: {weatherInfo.Main.Temp}°C";
                    await bot.SendTextMessageAsync(message.Chat.Id, response);
                }
                else
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "Извините, не удалось получить данные о погоде.");
                }
            }
            else if (message.Text.StartsWith("/mytemperature"))
            {
                if (message.Location != null)
                {
                    var weatherInfo = await GetWeatherInfoByLocation(message.Location.Latitude, message.Location.Longitude);

                    if (weatherInfo != null)
                    {
                        var response = $"Текущая температура у вас: {weatherInfo.Main.Temp}°C";
                        await bot.SendTextMessageAsync(message.Chat.Id, response);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(message.Chat.Id, "Извините, не удалось получить данные о погоде.");
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "Для получения температуры укажите свою геолокацию.");
                }
            }
        }
    
        private static async Task<WeatherResponse> GetWeatherInfoByLocation(double latitude, double longitude)
        {
            var url = $"http://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid=ca085266e256c19f8ad8a74dbcfe86e2&units=metric";

            try
            {
                using var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherResponse>(responseContent);
                }
                else
                {
                    Console.WriteLine($"Failed to fetch weather data. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching weather data: {ex.Message}");
                return null;
            }
        }
        private static async Task<WeatherResponse> GetWeatherInfo(string city)
        {
            var url = $"http://api.openweathermap.org/data/2.5/weather?q=Dnipro&appid=ca085266e256c19f8ad8a74dbcfe86e2&units=metric";

            try
            {
                using var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherResponse>(responseContent);
                }
                else
                {
                    Console.WriteLine($"Failed to fetch weather data. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching weather data: {ex.Message}");
                return null;
            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken token)
        {
            var ErrorMessage = ex switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => ex.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }

    class WeatherResponse
    {
        public WeatherMain Main { get; set; }
    }

    class WeatherMain
    {
        public float Temp { get; set; }
    }
}