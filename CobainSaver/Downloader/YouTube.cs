using CobainSaver.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoLibrary;
using YoutubeDLSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace CobainSaver.Downloader
{
    internal class YouTube
    {
        public async Task YoutubeDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            Stream stream = null;
            //превью видео
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                var youtube = new YoutubeClient();
                var allInfo = await youtube.Videos.GetAsync(normallMsg);

                var videoUrl = normallMsg;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                string size = streamInfo.Size.MegaBytes.ToString();
                if (Convert.ToDouble(size) >= 50)
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
                stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                string duration = allInfo.Duration.Value.TotalSeconds.ToString();
                string title = allInfo.Title;
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                string thumbnail = "https://img.youtube.com/vi/" + allInfo.Id + "/maxresdefault.jpg";

                string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                if (!Directory.Exists(audioPath))
                {
                    Directory.CreateDirectory(audioPath);
                }
                string videoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "video.mp4");
                string thumbnailVideoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                using (var client = new WebClient())
                {
                    client.DownloadFile(streamInfo.Url, videoPath);
                }
                using (var client = new WebClient())
                {
                    client.DownloadFile(thumbnail, thumbnailVideoPath);
                }
                await using Stream streamVideo = System.IO.File.OpenRead(videoPath);
                await using Stream streamThumbVideo = System.IO.File.OpenRead(thumbnailVideoPath);

                try
                {
                    // Отправляем видео обратно пользователю
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        caption: title,
                        video: InputFile.FromStream(streamVideo),
                        thumbnail: InputFile.FromStream(streamThumbVideo),
                        duration: Convert.ToInt32(duration),
                        replyToMessageId: update.Message.MessageId);
                    await addDB.AddBotCommands(chatId, "youtube", DateTime.Now.ToShortDateString());
                }
                catch(Exception ex)
                {
                    //await Console.Out.WriteLineAsync(ex.ToString());
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this video has a problem: the video has an age restriction",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим відео виникла помилка: відео має вікові обмеження",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим видео возникли проблемы: видео имеет возрастное ограничение",
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
                streamThumbVideo.Close();
                System.IO.File.Delete(videoPath);
                System.IO.File.Delete(thumbnailVideoPath);
            }
            catch (Exception ex)
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
                await YoutubeDownloaderReserve(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);
                //throw;
            }
            finally
            {

            }
        }
        public async Task YoutubeDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                string url = await DeleteNotUrl(messageText);

                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = jsonObjectAPI["ffmpegPath"][1].ToString();
                ytdl.FFmpegPath = jsonObjectAPI["ffmpegPath"][0].ToString();

                string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                if (!Directory.Exists(audioPath))
                {
                    Directory.CreateDirectory(audioPath);
                }
                string pornPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "VIDEO.webm");
                string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVIDEO.jpeg");


                ytdl.OutputFileTemplate = pornPath;

                var res = await ytdl.RunVideoDataFetch(url);
                string title = res.Data.Title;
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                JObject jsonObject = JObject.Parse(res.Data.ToString());
                string thumbnail = jsonObject["thumbnail"].ToString();
                string duration = jsonObject["duration"].ToString();
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
                        duration: Convert.ToInt32(duration),
                        replyToMessageId: update.Message.MessageId
                    );

                    await addDB.AddBotCommands(chatId, "youtube", DateTime.Now.ToShortDateString());

                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
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
                //Console.WriteLine(ex.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, this video has a problem: the video has an age restriction\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, this video has a problem: the video has an age restriction\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, this video has a problem: the video has an age restriction\n" +
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
        public async Task YoutubeMusicDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            Stream stream = null;
            //превью видео
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                var youtube = new YoutubeClient();
                var allInfo = await youtube.Videos.GetAsync(normallMsg);

                var videoUrl = normallMsg;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                string size = streamInfo.Size.MegaBytes.ToString();
                if (Convert.ToDouble(size) >= 50)
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this audio has a problem: the audio is too big (the size should not exceed 50mb)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим аудіо виникла помилка: аудіо занадто велике (розмір має не перевищувати 50мб)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим аудио возникли проблемы: аудио слишком большое(размер должен не превышать 50мб)",
                            replyToMessageId: update.Message.MessageId);
                    }
                    return;
                }
                stream = await youtube.Videos.Streams.GetAsync(streamInfo);

                string title = allInfo.Title;
                string thumbnail = "https://img.youtube.com/vi/" + allInfo.Id + "/maxresdefault.jpg";
                string duration = allInfo.Duration.Value.TotalSeconds.ToString();
                string author = allInfo.Author.ToString();

                string path = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string audioPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "audio.mp3");
                string thumbnailAudioPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(streamInfo.Url, audioPath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailAudioPath);
                    }
                }
                catch (Exception e)
                {
                    //await Console.Out.WriteLineAsync(e.ToString());
                }
                if (!System.IO.File.Exists(thumbnailAudioPath))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbnailAudioPath);
                    }
                }
                await using Stream streamAudio = System.IO.File.OpenRead(audioPath);
                await using Stream streamThumbAudio = System.IO.File.OpenRead(thumbnailAudioPath);
                // Отправляем видео обратно пользователю
                try
                {
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        title: title,
                        audio: InputFile.FromStream(streamAudio),
                        performer: author,
                        thumbnail: InputFile.FromStream(streamThumbAudio),
                        duration: Convert.ToInt32(duration),
                        replyToMessageId: update.Message.MessageId); ;
                    await addDB.AddBotCommands(chatId, "youtubeMusic", DateTime.Now.ToShortDateString());
                }
                catch(Exception ex)
                { //await Console.Out.WriteLineAsync(ex.ToString());
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this type of audio is not supported, only send me public content",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент",
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

                streamAudio.Close();
                streamThumbAudio.Close();
                System.IO.File.Delete(audioPath);
                System.IO.File.Delete(thumbnailAudioPath);
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
                        text: "Sorry, this type of audio is not supported, only send me public content",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент",
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
                //throw;
            }
            finally
            {

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
