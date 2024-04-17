using CobainSaver.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using YoutubeDLSharp;

namespace CobainSaver.Downloader
{
    internal class Pinterest
    {
        public async Task PinterestDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                string url = await DeleteNotUrl(messageText);

                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = jsonObjectAPI["ffmpegPath"][1].ToString();
                ytdl.FFmpegPath = jsonObjectAPI["ffmpegPath"][0].ToString();

                string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                if (!Directory.Exists(audioPath))
                {
                    Directory.CreateDirectory(audioPath);
                }
                string pornPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "VIDEO.MPEG4");
                string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVIDEO.jpeg");


                ytdl.OutputFileTemplate = pornPath;

                var res = await ytdl.RunVideoDataFetch(url);
                if(res.Data == null)
                {
                    await PinterestDownloaderPhoto(chatId, update, cancellationToken, messageText, botClient);
                    return;
                }
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                string title = res.Data.Title;
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                JObject jsonObject = JObject.Parse(res.Data.ToString());
                string thumbnail = jsonObject["thumbnail"].ToString();
                int duration = Convert.ToInt32(jsonObject["duration"]);
                if(duration >= 300)
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
                await using Stream streamVideo = System.IO.File.OpenRead(pornPath);
                await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);

                try
                {
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(streamVideo),
                        thumbnail: InputFile.FromStream(streamThumb),
                        caption: title,
                        disableNotification: false,
                        duration: duration,
                        replyToMessageId: update.Message.MessageId
                    );

                    await addDB.AddBotCommands(chatId, "pinterest", DateTime.Now.ToShortDateString());
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, story not found or content is private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, сторі не знайдено або контент є приватним\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, история не найдена или контент является приватным\n" +
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
        public async Task PinterestDownloaderPhoto(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                AddToDataBase addDB = new AddToDataBase();
                string url = await DeleteNotUrl(messageText);
                string searchResult = await Search(url);
                string[] splitArray = searchResult.Split(new string[] { "href=\"https://i." }, StringSplitOptions.None);

                if (splitArray.Length > 1)
                {
                    string[] splitArray2 = splitArray[1].Split(new string[] { "\" as=\"image\"/>" }, StringSplitOptions.None);
                    string imageUrl = "https://i." + splitArray2[0] + ".jpg";

                    string urlStartMarker = "https://i.pinimg.com/";
                    string urlEndMarker = ".jpg";

                    int startIndex = imageUrl.IndexOf(urlStartMarker);
                    if (startIndex == -1)
                        imageUrl = string.Empty;

                    startIndex += urlStartMarker.Length;

                    int endIndex = imageUrl.IndexOf(urlEndMarker, startIndex);
                    if (endIndex == -1)
                        imageUrl = string.Empty;

                    imageUrl = "https://i.pinimg.com/" + imageUrl.Substring(startIndex, endIndex - startIndex + urlEndMarker.Length);

                    await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: InputFile.FromUri(imageUrl),
                        disableNotification: false,
                        replyToMessageId: update.Message.MessageId
                    );
                    await addDB.AddBotCommands(chatId, "pinterest", DateTime.Now.ToShortDateString());
                }
                else
                {
                }
            }
            catch(Exception ex)
            {
                //Console.WriteLine(ex.ToString());
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
                await PinterestDownloaderPhotoReserve(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task PinterestDownloaderPhotoReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                AddToDataBase addDB = new AddToDataBase();
                string url = await DeleteNotUrl(messageText);
                string searchResult = await Search(url);
                string[] splitArray = searchResult.Split(new string[] { "href=\"https://i." }, StringSplitOptions.None);

                if (splitArray.Length > 1)
                {
                    string[] splitArray2 = splitArray[1].Split(new string[] { "\" as=\"image\"/>" }, StringSplitOptions.None);
                    string imageUrl = "https://i." + splitArray2[0] + ".jpg";

                    string urlStartMarker = "https://i.pinimg.com/";
                    string urlEndMarker = ".jpg";

                    int startIndex = imageUrl.IndexOf(urlStartMarker);
                    if (startIndex == -1)
                        imageUrl = string.Empty;

                    startIndex += urlStartMarker.Length;

                    int endIndex = imageUrl.IndexOf(urlEndMarker, startIndex);
                    if (endIndex == -1)
                        imageUrl = string.Empty;

                    imageUrl = "https://i.pinimg.com/" + imageUrl.Substring(startIndex, endIndex - startIndex + urlEndMarker.Length);


                    string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio"; // Укажите путь к директории, где хотите сохранить изображение

                    // Проверяем существует ли указанная директория
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory); // Создаем директорию, если она не существует
                    }

                    string fileName = DateTime.Now.Millisecond.ToString() + Path.GetFileName(imageUrl);
                    string filePath = Path.Combine(directory, fileName);

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(imageUrl, filePath);
                    }

                    await using Stream stream = System.IO.File.OpenRead(filePath);

                    await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: InputFile.FromStream(stream),
                        disableNotification: false,
                        replyToMessageId: update.Message.MessageId
                    );
                    await addDB.AddBotCommands(chatId, "pinterest", DateTime.Now.ToShortDateString());
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();

                    stream.Close();
                    System.IO.File.Delete(filePath);
                }
                else
                {
                }
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
                        text: "Sorry, story not found or content is private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, сторі не знайдено або контент є приватним\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, история не найдена или контент является приватным\n" +
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
        public async Task<string> Search(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
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
    }
}
