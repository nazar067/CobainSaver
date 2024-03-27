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
using VideoLibrary;
using YoutubeDLSharp;

namespace CobainSaver.Downloader
{
    internal class TikTok
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        private static readonly HttpClient client = new HttpClient();
        public async Task TikTokDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                string normallMsg = await DeleteNotUrl(messageText);
                var values = new Dictionary<string, string>
                {
                    { "url",  normallMsg},
                    { "hd", "1" }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(jsonObjectAPI["TTAPI"][0].ToString(), content);

                var responseString = await response.Content.ReadAsStringAsync();
                //await Console.Out.WriteLineAsync(responseString);
                JObject jsonObject = JObject.Parse(responseString);
                long size = 0;
                try
                {
                    size = Convert.ToInt64(jsonObject["data"]["size"].ToString());
                }
                catch
                {
                }
                if (size > 52428800)
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
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["data"]["images"] != null)
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
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
                            disableNotification: true,
                            replyToMessageId: update.Message.MessageId); ;
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string title = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string filePath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "audio.mp3");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);

                    try
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: title,
                            duration: duration,
                            disableNotification: true,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    catch (Exception ex)
                    {
                        await TikTokDownloaderReserve(chatId, update, cancellationToken, messageText, botClient);
                    }

                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
                else
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string video = jsonObject["data"]["play"].ToString();
                    string title = jsonObject["data"]["title"].ToString();
                    if (title.Contains("#"))
                    {
                        title = Regex.Replace(title, @"#.*", "");
                    }
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromUri(video),
                        caption: title,
                        disableNotification: true,
                        replyToMessageId: update.Message.MessageId
                        );
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string musicTitle = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string filePath = Path.Combine(audioPath, chatId + "audio.mp3");
                    string thumbnailPath = Path.Combine(audioPath, chatId + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);

                    try
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: musicTitle,
                            duration: duration,
                            disableNotification: true,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    catch (Exception ex)
                    {
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
                        await TikTokDownloaderReserve(chatId, update, cancellationToken, messageText, botClient);
                    }

                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
            }
            catch (Exception ex)
            {
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
                await TikTokDownloaderReserve(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task TikTokDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                string normallMsg = await DeleteNotUrl(messageText);
                var values = new Dictionary<string, string>
                {
                    { "url",  normallMsg},
                    { "hd", "1" }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(jsonObjectAPI["TTAPI"][0].ToString(), content);

                var responseString = await response.Content.ReadAsStringAsync();
                //await Console.Out.WriteLineAsync(responseString);
                JObject jsonObject = JObject.Parse(responseString);
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["data"]["images"] != null)
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
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
                            disableNotification: true,
                            replyToMessageId: update.Message.MessageId); ;
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string title = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string filePath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "audio.mp3");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    try
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: title,
                            duration: duration,
                            disableNotification: true,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
                        var message = update.Message;
                        var user = message.From;
                        var chat = message.Chat;
                        Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                        await logs.WriteServerLogs();
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
                                text: "Sorry, I need more time to process this content, your video or photo will load in a moment",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, для опрацювання цього контенту мені потрібно більше часу, за мить ваше відео або фото завантажаться",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, для обработки данного контента мне нужно больше времени, через мгновение ваше видео или фото загрузятся ",
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
                        await TikTokDownloaderReserveAPI(chatId, update, cancellationToken, messageText, botClient);
                    }

                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
                else
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string video = jsonObject["data"]["play"].ToString();
                    string title = jsonObject["data"]["title"].ToString();
                    int videoDuration = Convert.ToInt32(jsonObject["data"]["duration"]);
                    string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";

                    string videoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "video.mp4");
                    string thumbnailVideo = jsonObject["data"]["origin_cover"].ToString();
                    string thumbnailVideoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(video, videoPath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnailVideo, thumbnailVideoPath);
                    }
                    await using Stream streamVideo = System.IO.File.OpenRead(videoPath);
                    await using Stream streamThumbVideo = System.IO.File.OpenRead(thumbnailVideoPath);

                    if (title.Contains("#"))
                    {
                        title = Regex.Replace(title, @"#.*", "");
                    }

                    try
                    {
                        await botClient.SendVideoAsync(
                            chatId: chatId,
                            video: InputFile.FromStream(streamVideo),
                            caption: title,
                            disableNotification: true,
                            duration: videoDuration,
                            thumbnail: InputFile.FromStream(streamThumbVideo),
                            replyToMessageId: update.Message.MessageId
                            );
                        var message = update.Message;
                        var user = message.From;
                        var chat = message.Chat;
                        Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                        await logs.WriteServerLogs();
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
                                text: "Sorry, I need more time to process this content, your video or photo will load in a moment",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, для опрацювання цього контенту мені потрібно більше часу, за мить ваше відео або фото завантажаться",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, для обработки данного контента мне нужно больше времени, через мгновение ваше видео или фото загрузятся ",
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
                        await TikTokDownloaderReserveAPI(chatId, update, cancellationToken, messageText, botClient);
                    }

                    streamVideo.Close();
                    streamThumbVideo.Close();
                    System.IO.File.Delete(videoPath);
                    System.IO.File.Delete(thumbnailVideoPath);
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string musicTitle = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string filePath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "audio.mp3");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);

                    try
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: musicTitle,
                            duration: duration,
                            disableNotification: true,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
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
                                text: "Sorry, I need more time to process this content, your video or photo will load in a moment",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, для опрацювання цього контенту мені потрібно більше часу, за мить ваше відео або фото завантажаться",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, для обработки данного контента мне нужно больше времени, через мгновение ваше видео или фото загрузятся ",
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
                        await TikTokDownloaderReserveAPI(chatId, update, cancellationToken, messageText, botClient);
                    }

                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
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
                        text: "Sorry, I need more time to process this content, your video or photo will load in a moment",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, для опрацювання цього контенту мені потрібно більше часу, за мить ваше відео або фото завантажаться",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, для обработки данного контента мне нужно больше времени, через мгновение ваше видео или фото загрузятся ",
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
                await TikTokDownloaderReserveAPI(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task TikTokDownloaderReserveAPI(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
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
                string pornPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "VIDEO.mp4");
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
                    return;
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
        static List<List<T>> ConvertTo2D<T>(List<T> arr, int rowSize)
        {
            List<List<T>> result = new List<List<T>>();
            for (int i = 0; i < arr.Count; i += rowSize)
            {
                result.Add(arr.GetRange(i, Math.Min(rowSize, arr.Count - i)));
            }
            return result;
        }
    }
}
