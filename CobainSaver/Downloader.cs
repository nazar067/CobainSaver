using AngleSharp.Dom;
using AngleSharp.Io;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VideoLibrary;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace CobainSaver
{
    internal class Downloader
    {
        private static readonly HttpClient client = new HttpClient();
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };
        private static readonly HttpClient redditClient = new HttpClient(handler);
        public Downloader()
        {
            redditClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
        }
        public async Task YoutubeDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            Stream stream = null;
            //превью видео
            string thumbnail = null;
            if (normallMsg.Contains("youtu.be"))
            {
                string video_id = normallMsg.Remove(0, 17);
                thumbnail = "https://img.youtube.com/vi/" + video_id.Split('?').First() + "/maxresdefault.jpg";
            }
            else if (normallMsg.Contains("youtube.com"))
            {
                string video_id = normallMsg.Remove(0, 32);
                thumbnail = "https://img.youtube.com/vi/" + video_id + "/maxresdefault.jpg";
            }
            try
            {
                var youtube = new YoutubeClient();

                var videoUrl = normallMsg;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                    
                // Отправляем видео обратно пользователю
                await botClient.SendVideoAsync(
                    chatId: chatId,
                    video: InputFile.FromStream(stream),
                    thumbnail: InputFile.FromUri(thumbnail),
                    replyToMessageId: update.Message.MessageId);
            }
            catch (Exception)
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if(lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, this video has a problem: the video has an age restriction or video to long",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, це відео має проблему: відео має вікові обмеження або занадто довге відео",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, с этим видео возникли проблемы: видео имеет возрастное ограничение или видео слишком длинное",
                        replyToMessageId: update.Message.MessageId);
                }
            //throw;
            }
            finally
            {
                    
            }
        }
        public async Task TikTokDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient) 
        {
            string normallMsg = await DeleteNotUrl(messageText);
            var values = new Dictionary<string, string>
            {
                { "url",  normallMsg},
                { "hd", "1" }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://www.tikwm.com/api/", content);

            var responseString = await response.Content.ReadAsStringAsync();
            //await Console.Out.WriteLineAsync(responseString);
            JObject jsonObject = JObject.Parse(responseString);
            List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
            if (jsonObject["data"]["images"] != null)
            {
                foreach (var album in jsonObject["data"]["images"])
                {
                    mediaAlbum.Add(
                         new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                        );
                }
                int rowSize = 10;
                List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
                foreach (var item in result)
                {
                    await botClient.SendMediaGroupAsync(
                        chatId: chatId,
                        media: item,
                        replyToMessageId: update.Message.MessageId); ;
                }
                string music = jsonObject["data"]["music"].ToString();
                await botClient.SendAudioAsync(
                    chatId: chatId,
                    audio: InputFile.FromUri(music),
                    replyToMessageId: update.Message.MessageId
                    );
            }
            else
            {
                string video = jsonObject["data"]["play"].ToString();
                await botClient.SendVideoAsync(
                    chatId: chatId,
                    video: InputFile.FromUri(video),
                    replyToMessageId: update.Message.MessageId
                    );
            }
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
            string url = "https://api.reddit.com/api/info/?id=t3_" + postId;

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
                ProcessStartInfo startInfo = new ProcessStartInfo("C:\\bin\\ffmpeg.exe");
                startInfo.Arguments = $"\"-reconnect\" \"1\" \"-reconnect_streamed\" \"1\" \"-reconnect_delay_max\" \"5\" -c:a aac -strict experimental \"-loglevel\" \"warning\" -i \"{video}\" -i \"{audio}\" \"{result}\"";
                var proc = System.Diagnostics.Process.Start(startInfo);
                proc.WaitForExit();
                await using Stream stream = System.IO.File.OpenRead(result);
                string caption = jsonObject["data"]["children"][0]["data"]["title"].ToString();
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
        public async Task TwitterDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            string mediaId = null;
            if (normallMsg.Contains("https://x.com/"))
            {
                mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                mediaId = mediaId.Split('?').First();
            }
            else if (normallMsg.Contains("https://twitter.com/")){
                mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                if (mediaId.Contains("photo/"))
                {
                    mediaId = normallMsg.Remove(normallMsg.LastIndexOf("/"));
                }
            }
            var response = await client.GetAsync("https://api.vxtwitter.com/Twitter/status/" + mediaId);
            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(responseString);
            string caption = jsonObject["text"].ToString();
            List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
            foreach (var album in jsonObject["mediaURLs"])
            {
                if (album.ToString().Contains("/media/"))
                {
                    mediaAlbum.Add(
                         new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                         {
                             Caption = caption,
                             ParseMode = ParseMode.Markdown
                         }
                        );
                }
                else if (album.ToString().Contains("video."))
                {
                    if (!jsonObject["mediaURLs"].ToString().Contains("/media/"))
                    {
                        mediaAlbum.Add(
                             new InputMediaVideo(InputFile.FromUri(album.ToString()))
                             {
                                 Caption = caption,
                                 ParseMode = ParseMode.Markdown
                             }
                            );
                    }
                    else
                    {
                        mediaAlbum.Add(
                             new InputMediaVideo(InputFile.FromUri(album.ToString()))
                            );
                    }

                }
            }
            int rowSize = 10;
            List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
            foreach (var item in result)
            {
                await botClient.SendMediaGroupAsync(
                    chatId: chatId,
                    media: item,
                    replyToMessageId: update.Message.MessageId);
            }
        }
        public async Task InstagramDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            var response = await client.GetAsync(messageText + "&__a = 1");
            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(responseString);
            await Console.Out.WriteLineAsync(jsonObject.ToString());
        }
        static List<List<T>> ConvertTo2D<T>(List<T> arr, int rowSize)
        {
            List<List<T>> result = new List<List<T>>();
            for (int i = 0; i < arr.Count; i += rowSize)
            {
                result.Add(arr.GetRange(i, Math.Min(rowSize, arr.Count - i)));
            }
            return result;
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
            Regex regex = new Regex(@"\bhttps://\S+\b");

            // Находим первое совпадение
            Match match = regex.Match(message);

            // Если найдено совпадение, возвращаем найденный URL
            return match.Value;

        }
    }
}
