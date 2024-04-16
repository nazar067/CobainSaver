using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using VideoLibrary;
using System.Text.RegularExpressions;
using CobainSaver.DataBase;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using YoutubeExplode.Videos;
using YoutubeDLSharp.Metadata;
using YoutubeExplode.Common;

namespace CobainSaver.Downloader
{
    internal class Twitter
    {
        private static readonly HttpClient client = new HttpClient();
        public async Task TwitterDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                string normallMsg = await DeleteNotUrl(messageText);
                string mediaId = null;
                if (normallMsg.Contains("https://x.com/"))
                {
                    mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                    mediaId = mediaId.Split('?').First();
                }
                else if (normallMsg.Contains("https://twitter.com/"))
                {
                    mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                    if (mediaId.Contains("photo/"))
                    {
                        mediaId = normallMsg.Remove(normallMsg.LastIndexOf("/"));
                    }
                }
                var response = await client.GetAsync(jsonObjectAPI["XAPI"][0] + mediaId);
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);
                string caption = jsonObject["text"].ToString();
                if (caption.Length > 1024)
                {
                    caption = caption.Substring(0, 1024) + "...";
                }
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                foreach (var album in jsonObject["mediaURLs"])
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
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
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
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
                await addDB.AddBotCommands(chatId, "twitter", DateTime.Now.ToShortDateString());
            }
            catch(Exception ex)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
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
                await TwitterDownloaderReserve(chatId, update, cancellationToken, messageText, botClient);
            }
            
        }
        public async Task TwitterDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                string normallMsg = await DeleteNotUrl(messageText);
                string mediaId = null;
                if (normallMsg.Contains("https://x.com/"))
                {
                    mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                    mediaId = mediaId.Split('?').First();
                }
                else if (normallMsg.Contains("https://twitter.com/"))
                {
                    mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                    if (mediaId.Contains("photo/"))
                    {
                        mediaId = normallMsg.Remove(normallMsg.LastIndexOf("/"));
                    }
                }
                string videoPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                if (!Directory.Exists(videoPath))
                {
                    Directory.CreateDirectory(videoPath);
                }
                var response = await client.GetAsync(jsonObjectAPI["XAPI"][0] + mediaId);
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);
                string caption = jsonObject["text"].ToString();
                int duration = Convert.ToInt32(jsonObject["media_extended"][0]["duration_millis"]) / 1000;
                string thumbnail = jsonObject["media_extended"][0]["thumbnail_url"].ToString();
                if (caption.Length > 1024)
                {
                    caption = caption.Substring(0, 1020) + "...";
                }
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                foreach (var album in jsonObject["mediaURLs"])
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
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
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                        string filePath = Path.Combine(videoPath, DateTime.Now.Millisecond.ToString() + "video.mp3");
                        string thumbPath = Path.Combine(videoPath, DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(album.ToString(), filePath);
                        }
                        try
                        {
                            using (var client = new WebClient())
                            {
                                client.DownloadFile(thumbnail, thumbPath);
                            }
                        }
                        catch (Exception e)
                        {
                            //await Console.Out.WriteLineAsync(e.ToString());
                        }
                        if (!System.IO.File.Exists(thumbPath))
                        {
                            using (var client = new WebClient())
                            {
                                client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbPath);
                            }
                        }
                        await using Stream stream = System.IO.File.OpenRead(filePath);
                        await using Stream streamThumb = System.IO.File.OpenRead(thumbPath);
                        try
                        {
                            await botClient.SendVideoAsync(
                                chatId: chatId,
                                video: InputFile.FromStream(stream),
                                caption: caption,
                                duration: duration,
                                thumbnail: InputFile.FromStream(streamThumb),
                                disableNotification: true,
                                replyToMessageId: update.Message.MessageId
                                );
                        }
                        catch (Exception ex)
                        {
                            Language language = new Language("rand", "rand");
                            string lang = await language.GetCurrentLanguage(chatId.ToString());
                            if (lang == "eng")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Sorry, this video has a problem: the video is too big (the size should not exceed 50mb)",
                                    replyToMessageId: update.Message.MessageId);
                            }
                            if (lang == "ukr")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)",
                                    replyToMessageId: update.Message.MessageId);
                            }
                            if (lang == "rus")
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)",
                                    replyToMessageId: update.Message.MessageId);
                            }
                        }
                        stream.Close();
                        streamThumb.Close();
                        System.IO.File.Delete(filePath);
                        System.IO.File.Delete(thumbPath);
                        await addDB.AddBotCommands(chatId, "twitter", DateTime.Now.ToShortDateString());
                        return;
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
                await addDB.AddBotCommands(chatId, "twitter", DateTime.Now.ToShortDateString());
            }
            catch (Exception ex)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, this content is not available or hidden to me\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, цей контент недоступний або прихований для мене\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, данный контент недоступен или скрыт для меня\n" +
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
                }
            }
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
