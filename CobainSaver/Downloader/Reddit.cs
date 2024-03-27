 using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Text.RegularExpressions;
using System.Net;

namespace CobainSaver.Downloader
{
    internal class Reddit
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        static string proxyURL = jsonObjectAPI["Proxy"][0].ToString();
        static string proxyUsername = jsonObjectAPI["Proxy"][1].ToString();
        static string proxyPassword = jsonObjectAPI["Proxy"][2].ToString();
        static string torProxyUrl = jsonObjectAPI["Proxy"][3].ToString();

        static WebProxy webProxy = new WebProxy
        {
            Address = new Uri(proxyURL),
            // specify the proxy credentials
            Credentials = new NetworkCredential(
                userName: proxyUsername,
                password: proxyPassword
          )
        };
        static WebProxy torProxy = new WebProxy
        {
            Address = new Uri(torProxyUrl),
        };
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = torProxy,
            UseCookies = false
        };
        private static readonly HttpClient redditClient = new HttpClient(handler);
        public Reddit()
        {
            redditClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
        }
        public async Task ReditDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            string postId = await GetPostId(normallMsg);
            if (postId == null)
            {
                await Console.Out.WriteLineAsync("null");
                return;
            }
            string url = jsonObjectAPI["RedditAPI"][0] + postId;

            var response = await redditClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(responseString);
            if (!jsonObject["data"]["children"].Any())
            {
                await Console.Out.WriteLineAsync("json null");
                return;
            }
            if (!jsonObject["data"]["children"][0]["data"]["media"].Any())
            {
                string photoUrl = jsonObject["data"]["children"][0]["data"]["url"].ToString();
                string caption = jsonObject["data"]["children"][0]["data"]["title"].ToString();
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromUri(photoUrl),
                    caption: caption,
                    replyToMessageId: update.Message.MessageId
                    );
            }
            else
            {
                string fallbackUrl = jsonObject["data"]["children"][0]["data"]["media"]["reddit_video"]["fallback_url"].ToString();

                string video = fallbackUrl.Split('?').First();

                string audio = video.Remove(video.LastIndexOf("/"));
                audio = audio + "/DASH_AUDIO_128.mp4";
                string result = Path.GetTempPath();
                DateTime currentTime = DateTime.UtcNow;
                int unixTime = Convert.ToInt32(((DateTimeOffset)currentTime).ToUnixTimeSeconds());
                result += $"video-{unixTime}.mp4";
                ProcessStartInfo startInfo = new ProcessStartInfo(jsonObjectAPI["ffmpegPath"][0].ToString());
                //startInfo.Arguments = $"\"-reconnect\" \"1\" \"-reconnect_streamed\" \"1\" \"-reconnect_delay_max\" \"5\" -c:a aac -strict experimental \"-loglevel\" \"warning\" -i \"{video}\" -i \"{audio}\" \"{result}\"";
                startInfo.Arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i {video} -i {audio} -c:v libx264 -b:v 1500k -c:a aac -b:a 128k -strict experimental -loglevel warning {result}";
                var proc = System.Diagnostics.Process.Start(startInfo);
                proc.WaitForExit();
                await using Stream stream = System.IO.File.OpenRead(result);
                string caption = jsonObject["data"]["children"][0]["data"]["title"].ToString();
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                if (caption != null)
                {
                    await botClient.SendVideoAsync(
                            chatId: chatId,
                            video: InputFile.FromStream(stream),
                            caption: caption,
                            replyToMessageId: update.Message.MessageId
                            );
                }
                else
                {
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(stream),
                        replyToMessageId: update.Message.MessageId
                        );
                }
                stream.Close();
                System.IO.File.Delete(result);
            }

        }
        public async Task<string> GetPostId(string postUrl)
        {
            var url = new Uri(postUrl);
            string stringUrl = null;
            //https://redd.it/193mrjr
            if (postUrl.StartsWith("https://redd.it/"))
            {
                stringUrl = url.LocalPath.Split('/')[1];
            }
            //https://www.reddit.com/comments/193mrjr
            if (url.LocalPath.StartsWith("/comments/"))
            {
                stringUrl = url.LocalPath.Split('/')[2];
            }
            //https://www.reddit.com/r/SweatyPalms/s/DAqASCU2nX
            if (url.LocalPath.Split('/').Contains("s"))
            {
                HttpRequestMessage request = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, postUrl);
                HttpResponseMessage response = await redditClient.SendAsync(request);
                string location = response.Headers.Location.ToString();
                return await GetPostId(location);
            }
            if (url.LocalPath.Split('/').Contains("comments"))
            {
                stringUrl = url.LocalPath.Split('/')[4];
            }
            return stringUrl;
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
    }
}
