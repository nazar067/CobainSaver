using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using YoutubeSearchApi.Net.Models.Youtube;
using YoutubeSearchApi.Net.Services;
using System.Text.RegularExpressions;

namespace CobainSaver.Downloader
{
    internal class Spotify
    {
        public async Task SpotifyGetName(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                HttpClient spotifyClient = new HttpClient();

                string client_id = jsonObjectAPI["Spotify"][0].ToString();
                string client_secret = jsonObjectAPI["Spotify"][1].ToString();

                string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));

                string url = await DeleteNotUrl(messageText);

                string id = await ExtractTrackId(url);

                var authOptions = new
                {
                    url = "https://accounts.spotify.com/api/token",
                    headers = new
                    {
                        Authorization = "Basic " + base64Auth
                    },
                    form = new
                    {
                        grant_type = "client_credentials"
                    }
                };

                string accessToken = await GetAccessToken(authOptions, base64Auth);
                spotifyClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var spotify = new SpotifyClient(accessToken);

                var track = await spotify.Tracks.Get(id);
                string artist = null;
                foreach (var artists in track.Artists)
                {
                    artist = artists.Name;
                }
                await FindSongYTMusic(track.Name + " " + artist, chatId, update, cancellationToken, messageText, botClient);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry this song was not found or private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте ця пісня не знайдена або приватна\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините эта песня не найдена или приватная\n" +
                        "\nЕсли вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        public async Task FindSongYTMusic(string info, long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            YouTube youTube = new YouTube();
            using (var httpClient = new HttpClient())
            {
                YoutubeSearchClient client = new YoutubeSearchClient(httpClient);

                var responseObject = await client.SearchAsync(info);

                string videoId = null;

                foreach (YoutubeVideo video in responseObject.Results)
                {
                    videoId = video.Id;
                    break;
                }
                string song = "spotify " + "https://music.youtube.com/watch?v=" + videoId;

                await youTube.YoutubeMusicDownloader(chatId, update, cancellationToken, song, botClient);
            }
        }
        static async Task<string> GetAccessToken(dynamic authOptions, string base64Auth)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Auth);

                var response = await client.PostAsync(authOptions.url, content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                    return json.access_token;
                }
                else
                {
                    Console.WriteLine("Ошибка при запросе токена: " + response.StatusCode);
                    return null;
                }
            }
        }
        public async Task<string> DeleteNotUrl(string message)
        {
            // Регулярное выражение для URL-адресов
            Regex regexUrl = new Regex(@"\bhttps://\S+\b");

            // Регулярное выражение для коротких ссылок YouTube
            Regex regexShortUrl = new Regex(@"youtu.be/\w+");

            // Поиск URL-адреса
            Match matchUrl = regexUrl.Match(message);

            // Поиск короткой ссылки YouTube
            Match matchShortUrl = regexShortUrl.Match(message);

            // Если найден URL-адрес, возвращаем его
            if (matchUrl.Success)
            {
                return matchUrl.Value;
            }

            // Если найдена короткая ссылка YouTube, добавляем "https://" и возвращаем
            if (matchShortUrl.Success)
            {
                return "https://" + matchShortUrl.Value;
            }

            // Если не найдено ни одного совпадения, возвращаем пустую строку
            return string.Empty;
        }
        public async Task<string> EscapeMarkdownV2(string input)
        {
            // Dictionary of characters needing escaping and their replacements
            Dictionary<string, string> escapeCharacters = new Dictionary<string, string>
            {
                { "\\", "\\\\" },
                { "_", "\\_" },
                { "*", "\\*" },
                { "[", "\\[" },
                { "]", "\\]" },
                { "(", "\\(" },
                { ")", "\\)" },
                { "~", "\\~" },
                { "`", "\\`" },
                { ">", "\\>" },
                { "#", "\\#" },
                { "+", "\\+" },
                { "-", "\\-" },
                { "=", "\\=" },
                { "|", "\\|" },
                { "{", "\\{" },
                { "}", "\\}" },
                { ".", "\\." },
                { "!", "\\!" },
            };

            // Iterate through the dictionary and perform replacements using regular expressions
            foreach (var pair in escapeCharacters)
            {
                input = input.Replace(pair.Key, pair.Value);
            }

            return input;
        }
        public async Task<string> ExtractTrackId(string url)
        {
            // Используем регулярное выражение для извлечения части строки между "track/" и "?"
            string pattern = @"track/([^?]+)";
            Match match = Regex.Match(url, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty; // Возвращаем пустую строку, если совпадений не найдено
        }
    }
}
