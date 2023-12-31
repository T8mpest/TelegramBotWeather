﻿using System;
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
using Newtonsoft.Json;

namespace TelegramBotWeather

{
    class Program
    {
        private const string BotToken = "6154663885:AAFfH8MfyJ263NGUvEFipcTkLD1Hx46a2rY";
        private const string WeatherApiKey = "ca085266e256c19f8ad8a74dbcfe86e2";
        private static Dictionary<long, string> CityRequests = new Dictionary<long, string>();
        

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
                 await bot.SendStickerAsync(update.Message.Chat.Id, sticker: InputFile.FromUri("https://raw.githubusercontent.com/T8mpest/TelegramBotWeather/main/stickers/sticker1.webp"), allowSendingWithoutReply: true, cancellationToken: ct);
            }
            else if (message.Text.StartsWith("/temperature"))
            {
                CityRequests[message.Chat.Id] = "waiting_for_city"; // Сохраняем состояние ожидания названия города
                await bot.SendTextMessageAsync(message.Chat.Id, "Введите название города, чтобы получить прогноз погоды на 7 дней:");
            }
            else if (CityRequests.TryGetValue(message.Chat.Id, out var cityRequestState) && cityRequestState == "waiting_for_city")
            {
                CityRequests.Remove(message.Chat.Id); // Удаляем состояние запроса города

                var city = message.Text.Trim();
                Console.WriteLine($"Requested city: {city}"); // Выводим название города в консоль
                var weatherInfo = await GetWeatherInfo(city);

                if (weatherInfo != null)
                {
                    var response = $"Прогноз погоды в городе {city}:\n";

                    foreach (var forecast in weatherInfo.ForecastList)
                    {
                        var forecastDateTime = DateTimeOffset.FromUnixTimeSeconds(forecast.DateUnix);
                        int[] targetHours = { 6, 12, 15, 18 };

                        if (!targetHours.Contains(forecastDateTime.Hour))
                        {
                            continue;
                        }

                        
                        response += $"{forecastDateTime.LocalDateTime.ToShortDateString()} {forecastDateTime.LocalDateTime.ToShortTimeString()}: {forecast.Main.Temp}°C\n";
                    }

                    await bot.SendTextMessageAsync(message.Chat.Id, response);
                }
                else
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "Извините, но идите вы нахуй с такими запросами далбоебы блять!❤️");
                    await bot.SendStickerAsync(update.Message.Chat.Id, sticker: InputFile.FromString("https://raw.githubusercontent.com/T8mpest/TelegramBotWeather/main/stickers/topsticker.webp"), allowSendingWithoutReply: true, cancellationToken: ct);
                }
            }

            if (message.Text.StartsWith("/mytemperature"))
            {
                if (message.Location != null)
                {
                    var weatherInfo = await GetWeatherInfoByLocation(message.Location.Latitude, message.Location.Longitude);

                    if (weatherInfo != null)
                    {
                        var response = $"Текущая температура у вас: {weatherInfo.ForecastList}°C";
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
            Console.WriteLine($"Request URL: {url}");

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
            var url = $"http://api.openweathermap.org/data/2.5/forecast?q={city}&appid={WeatherApiKey}&units=metric";
            Console.WriteLine($"Request URL: {url}");
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
        [JsonProperty("list")]
        public List<WeatherForecast> ForecastList { get; set; }
    }

    class WeatherForecast
    {
        [JsonProperty("dt")]
        public long DateUnix { get; set; }

        [JsonProperty("main")]
        public TemperatureInfo Main { get; set; }
    }

    class TemperatureInfo
    {
        [JsonProperty("temp")]
        public float Temp { get; set; }
    }
}