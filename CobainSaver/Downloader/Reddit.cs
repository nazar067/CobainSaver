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
using CobainSaver.DataBase;
using YoutubeDLSharp;
using System.Xml;
using YoutubeDLSharp.Metadata;
using Xabe.FFmpeg;

namespace CobainSaver.Downloader
{
    internal class Reddit
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        static string torProxyUrl = jsonObjectAPI["Proxy"][3].ToString();

        static WebProxy torProxy = new WebProxy
        {
            Address = new Uri(torProxyUrl),
        };
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            //Proxy = torProxy,
            UseCookies = false
        };
        private static readonly HttpClient redditClient = new HttpClient(handler);
        public Reddit()
        {
            redditClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
        }
        public async Task ReditVideoDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                string url = await DeleteNotUrl(messageText);
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = jsonObjectAPI["ffmpegPath"][1].ToString();
                ytdl.FFmpegPath = jsonObjectAPI["ffmpegPath"][0].ToString();

                string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string uniqueId = chatId.ToString() + DateTime.Now.Millisecond.ToString();

                string pornPath = Path.Combine(tempPath, uniqueId + "VIDEO.mp4");
                string thumbnailPath = Path.Combine(tempPath, uniqueId + "thumbVIDEO.jpeg");


                ytdl.OutputFileTemplate = pornPath;

                var res = await ytdl.RunVideoDataFetch(url);
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                string title = null;
                try
                {
                    title = res.Data.Title;
                }
                catch
                {
                    await ReditPhotoDownloader(chatId, update, cancellationToken, messageText, botClient);
                    return;
                }
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                JObject jsonObject = JObject.Parse(res.Data.ToString());
                string thumbnail = jsonObject["thumbnail"].ToString();
                int duration = Convert.ToInt32(jsonObject["duration"]);
                if (duration >= 300)
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this clip has a problem: the clip is too big (the size should not exceed 50mb)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим кліпом виникла помилка: кліп занадто велике (розмір має не перевищувати 50мб)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим клипом возникли проблемы: клип слишком большое(размер должен не превышать 50мб)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    return;
                }
                await ytdl.RunVideoDownload(url);

                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);

                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                }
                catch (Exception e)
                {
                    //await Console.Out.WriteLineAsync(e.ToString());
                }
                if (!System.IO.File.Exists(thumbnailPath))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbnailPath);
                    }
                }

                string[] files = Directory.GetFiles(tempPath);

                string audioPath = null;

                foreach (string file in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                    // Если файл содержит уникальный идентификатор и заканчивается на .mp4
                    if (fileNameWithoutExtension.Contains(uniqueId) && file.EndsWith(".mp4"))
                    {
                        pornPath = file; // присваиваем путь для видео
                    }
                    // Если файл содержит уникальный идентификатор и заканчивается на .m4a
                    else if (fileNameWithoutExtension.Contains(uniqueId) && file.EndsWith(".m4a"))
                    {
                        audioPath = file; // присваиваем путь для аудио
                    }
                }

                var videoStream = await FFmpeg.GetMediaInfo(pornPath);
                var audioStream = await FFmpeg.GetMediaInfo(audioPath);

                var outputFile = Path.Combine(tempPath, $"{uniqueId}output.mp4");

                await FFmpeg.Conversions.New()
                    .AddStream(videoStream.VideoStreams.First())
                    .AddStream(audioStream.AudioStreams.First())
                    .SetOutput(outputFile)
                    .Start();

                await using Stream streamVideo = System.IO.File.OpenRead(outputFile);
                await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                try
                {
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(streamVideo),
                        thumbnail: InputFile.FromStream(streamThumb),
                        caption: await ads.ShowAds() + title,
                        disableNotification: false,
                        duration: duration,
                        parseMode: ParseMode.Html,
                        replyToMessageId: update.Message.MessageId
                    );

                    await addDB.AddBotCommands(chatId, "reddit", DateTime.Now.ToShortDateString());
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString()); ;
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this clip has a problem: the clip is too big (the size should not exceed 50mb)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим кліпом виникла помилка: кліп занадто велике (розмір має не перевищувати 50мб)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим клипом возникли проблемы: клип слишком большое(размер должен не превышать 50мб)",
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
                    }
                }
                streamVideo.Close();
                streamThumb.Close();

                System.IO.File.Delete(pornPath);
                System.IO.File.Delete(thumbnailPath);
                System.IO.File.Delete(outputFile);
                System.IO.File.Delete(audioPath);
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
                        text: "Sorry, video not found or content is private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, відеo не знайдено або контент є приватним\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, видео не найдена или контент является приватным\n" +
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
        public async Task ReditPhotoDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

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
                    await addDB.AddBotCommands(chatId, "reddit", DateTime.Now.ToShortDateString());
                }
            }
            catch (Exception ex)
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, photo not found or content is private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, фото не знайдено або контент є приватним\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, фото не найдена или контент является приватным\n" +
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
