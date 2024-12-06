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
using System.Net.Sockets;
using AngleSharp.Dom;
using System.Text.Json.Nodes;
using CobainSaver.DataBase;
using StackExchange.Redis;

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
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

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
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)",
                            replyParameters: update.Message.MessageId);
                    }
                    return;
                }
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["data"]["images"] != null)
                {
                    int count = 0;
                    string caption = jsonObject["data"]["title"].ToString();
                    if (caption.Contains("#"))
                    {
                        caption = Regex.Replace(caption, @"#.*", "");
                    }
                    if (caption.Length > 800)
                    {
                        caption = caption.Substring(0, 800) + "...";
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                    foreach (var album in jsonObject["data"]["images"])
                    {
                        if(count == 0)
                        {
                            mediaAlbum.Add(
                                 new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                                 {
                                     Caption = await ads.ShowAds() + caption,
                                     ParseMode = ParseMode.Html
                                 }
                                );
                            count++;
                        }
                        else
                        {
                            mediaAlbum.Add(
                                 new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                                );
                        }
                    }
                    int rowSize = 10;
                    List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
                    foreach (var item in result)
                    {
                        await botClient.SendMediaGroupAsync(
                            chatId: chatId,
                            media: item,
                            disableNotification: true,
                            replyParameters: update.Message.MessageId); ;
                    }

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string title = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: title,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
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
                    if (title.Length > 1020)
                    {
                        title = title.Substring(0, 1020) + "...";
                    }
                    var getAds = await ads.ShowAds();
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromUri(video),
                        caption: getAds +
                        title,
                        disableNotification: true,
                        replyParameters: update.Message.MessageId,
                        parseMode: ParseMode.Html
                        );

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string musicTitle = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: musicTitle,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
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
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

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
                    int count = 0;
                    string caption = jsonObject["data"]["title"].ToString();
                    if (caption.Contains("#"))
                    {
                        caption = Regex.Replace(caption, @"#.*", "");
                    }
                    if (caption.Length > 800)
                    {
                        caption = caption.Substring(0, 800) + "...";
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                    foreach (var album in jsonObject["data"]["images"])
                    {
                        if(count == 0)
                        {
                            mediaAlbum.Add(
                                 new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                                 {
                                     Caption = await ads.ShowAds() + caption,
                                     ParseMode = ParseMode.Html
                                 }
                            );
                            count++;
                        }
                        else
                        {
                            mediaAlbum.Add(
                                 new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                            );
                        }
                    }
                    int rowSize = 10;
                    List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
                    foreach (var item in result)
                    {
                        await botClient.SendMediaGroupAsync(
                            chatId: chatId,
                            media: item,
                            disableNotification: true,
                            replyParameters: update.Message.MessageId); ;
                    }


                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["data"]["music"].ToString();
                    string perfomer = jsonObject["data"]["music_info"]["author"].ToString();
                    string title = jsonObject["data"]["music_info"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["data"]["music_info"]["duration"]);
                    string thumbnail = jsonObject["data"]["music_info"]["cover"].ToString();

                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: title,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
                }
                else
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string video = jsonObject["data"]["play"].ToString();
                    string title = jsonObject["data"]["title"].ToString();
                    int videoDuration = Convert.ToInt32(jsonObject["data"]["duration"]);
                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");

                    string videoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "video.MPEG4");
                    string thumbnailVideo = jsonObject["data"]["origin_cover"].ToString();
                    string thumbnailVideoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(video, videoPath);
                    }
                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(thumbnailVideo, thumbnailVideoPath);
                        }
                    }
                    catch (Exception e)
                    {
                        //await Console.Out.WriteLineAsync(e.ToString());
                    }
                    if (!System.IO.File.Exists(thumbnailVideoPath))
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbnailVideoPath);
                        }
                    }
                    await using Stream streamVideo = System.IO.File.OpenRead(videoPath);
                    await using Stream streamThumbVideo = System.IO.File.OpenRead(thumbnailVideoPath);

                    if (title.Contains("#"))
                    {
                        title = Regex.Replace(title, @"#.*", "");
                    }
                    if (title.Length > 800)
                    {
                        title = title.Substring(0, 800) + "...";
                    }
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(streamVideo),
                        caption: await ads.ShowAds() + title,
                        disableNotification: true,
                        duration: videoDuration,
                        thumbnail: InputFile.FromStream(streamThumbVideo),
                        parseMode:ParseMode.Html,
                        replyParameters: update.Message.MessageId
                        );

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
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: musicTitle,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
                }
            }
            catch (Exception ex)
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
                await TikTokDownloaderReserveAPI(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task TikTokDownloaderReserveAPI(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                Ads ads = new Ads();
                AddToDataBase addDB = new AddToDataBase();
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                string url = await DeleteNotUrl(messageText);
                string id = await GetVideoId(url);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var response = await client.GetAsync(jsonObjectAPI["TTAPI"][1].ToString() + $"{id}&iid=7318518857994389254&device_id=7318517321748022790&channel=googleplay&app_name=musical_ly&version_code=300904&device_platform=android&device_type=ASUS_Z01QD&version=9");
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);

                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["aweme_list"][0]["image_post_info"] != null)
                {
                    int count = 0;
                    string caption = jsonObject["aweme_list"][0]["desc"].ToString();
                    if (caption.Contains("#"))
                    {
                        caption = Regex.Replace(caption, @"#.*", "");
                    }
                    if (caption.Length > 800)
                    {
                        caption = caption.Substring(0, 800) + "...";
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                    foreach (var album in jsonObject["aweme_list"][0]["image_post_info"]["images"])
                    {
                        if(count == 0)
                        {
                            mediaAlbum.Add(
                             new InputMediaPhoto(InputFile.FromUri(album["display_image"]["url_list"][0].ToString()))
                             {
                                 Caption = await ads.ShowAds() + caption,
                                 ParseMode = ParseMode.Html
                             }
                            );
                            count++;
                        }
                        else
                        {
                            mediaAlbum.Add(
                             new InputMediaPhoto(InputFile.FromUri(album["display_image"]["url_list"][0].ToString()))
                            );
                        }
                    }
                    int rowSize = 10;
                    List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
                    foreach (var item in result)
                    {
                        await botClient.SendMediaGroupAsync(
                            chatId: chatId,
                            media: item,
                            disableNotification: true,
                            replyParameters: update.Message.MessageId);
                    }
                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["uri"].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string title = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();
                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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

                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: title,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
                else
                {
                    if (Convert.ToInt32(jsonObject["aweme_list"][0]["video"]["download_addr"]["data_size"]) > 52428800)
                    {
                        Language language = new Language("rand", "rand");
                        string lang = await language.GetCurrentLanguage(chatId.ToString());
                        if (lang == "eng")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Sorry, this video has a problem: the video is too big (the size should not exceed 50mb)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                        return;
                    }

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string video = jsonObject["aweme_list"][0]["video"]["play_addr"]["url_list"][0].ToString();
                    string title = jsonObject["aweme_list"][0]["desc"].ToString();
                    if (title.Contains("#"))
                    {
                        title = Regex.Replace(title, @"#.*", "");
                    }
                    if (title.Length > 800)
                    {
                        title = title.Substring(0, 800) + "...";
                    }

                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromUri(video),
                        caption: await ads.ShowAds() + title,
                        disableNotification: true,
                        replyParameters: update.Message.MessageId
                    );

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["uri"].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string titleMusic = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();

                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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
                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: titleMusic,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
                }
            }
            catch (Exception ex)
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
                await TikTokDownloaderReserveAPIDownload(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task TikTokDownloaderReserveAPIDownload(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                Ads ads = new Ads();
                AddToDataBase addDB = new AddToDataBase();
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                string url = await DeleteNotUrl(messageText);
                string id = await GetVideoId(url);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var response = await client.GetAsync(jsonObjectAPI["TTAPI"][1].ToString() + $"{id}&iid=7318518857994389254&device_id=7318517321748022790&channel=googleplay&app_name=musical_ly&version_code=300904&device_platform=android&device_type=ASUS_Z01QD&version=9");
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);

                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["aweme_list"][0]["image_post_info"] != null)
                {
                    int count = 0;
                    string caption = jsonObject["aweme_list"][0]["desc"].ToString();
                    if (caption.Contains("#"))
                    {
                        caption = Regex.Replace(caption, @"#.*", "");
                    }
                    if (caption.Length > 800)
                    {
                        caption = caption.Substring(0, 800) + "...";
                    }
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                    foreach (var album in jsonObject["aweme_list"][0]["image_post_info"]["images"])
                    {
                        if(count == 0)
                        {
                            mediaAlbum.Add(
                             new InputMediaPhoto(InputFile.FromUri(album["display_image"]["url_list"][0].ToString()))
                             {
                                 Caption = await ads.ShowAds() + caption,
                                 ParseMode = ParseMode.Html
                             }
                            );
                        }
                        else
                        {
                            mediaAlbum.Add(
                             new InputMediaPhoto(InputFile.FromUri(album["display_image"]["url_list"][0].ToString()))
                            );
                        }
                    }
                    int rowSize = 10;
                    List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
                    foreach (var item in result)
                    {
                        await botClient.SendMediaGroupAsync(
                            chatId: chatId,
                            media: item,
                            disableNotification: true,
                            replyParameters: update.Message.MessageId);
                    }
                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["uri"].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string title = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();
                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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

                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: title,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
                else
                {
                    if (Convert.ToInt32(jsonObject["aweme_list"][0]["video"]["download_addr"]["data_size"]) > 52428800)
                    {
                        Language language = new Language("rand", "rand");
                        string lang = await language.GetCurrentLanguage(chatId.ToString());
                        if (lang == "eng")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Sorry, this video has a problem: the video is too big (the size should not exceed 50mb)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                        return;
                    }

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string video = jsonObject["aweme_list"][0]["video"]["play_addr"]["url_list"][0].ToString();
                    string title = jsonObject["aweme_list"][0]["desc"].ToString();
                    int videoDuration = Convert.ToInt32(jsonObject["aweme_list"][0]["video"]["duration"]);
                    if (title.Contains("#"))
                    {   
                        title = Regex.Replace(title, @"#.*", "");
                    }
                    if (title.Length > 800)
                    {
                        title = title.Substring(0, 800) + "...";
                    }
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");

                    string videoPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "video.MPEG4");
                    string thumbnailVideo = jsonObject["aweme_list"][0]["video"]["origin_cover"]["url_list"][0].ToString();
                    string thumbnailVideoPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(video, videoPath);
                    }
                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(thumbnailVideo, thumbnailVideoPath);
                        }
                    }
                    catch (Exception e)
                    {
                        //await Console.Out.WriteLineAsync(e.ToString());
                    }
                    if (!System.IO.File.Exists(thumbnailVideoPath))
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbnailVideoPath);
                        }
                    }

                    await using Stream streamVideo = System.IO.File.OpenRead(videoPath);
                    await using Stream streamThumbVideo = System.IO.File.OpenRead(thumbnailVideoPath);
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(streamVideo),
                        caption: await ads.ShowAds() + title,
                        disableNotification: true,
                        duration: videoDuration / 1000,
                        parseMode: ParseMode.Html,
                        thumbnail: InputFile.FromStream(streamThumbVideo),
                        replyParameters: update.Message.MessageId
                    );

                    streamVideo.Close();
                    streamThumbVideo.Close();
                    System.IO.File.Delete(videoPath);
                    System.IO.File.Delete(thumbnailVideoPath);

                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["uri"].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string titleAudio = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();
                    string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string cleanTitle = MakeValidFileName(title);
                    string filePath = Path.Combine(audioPath, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".M4A");
                    string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumb.jpeg");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(music, filePath);
                    }
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

                    await using Stream stream = System.IO.File.OpenRead(filePath);
                    await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        performer: perfomer,
                        title: titleAudio,
                        duration: duration,
                        disableNotification: true,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyParameters: update.Message.MessageId
                        );
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);

                    await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
                }
            }
            catch (Exception ex)
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
                await TikTokDownloaderReserveAPI2(chatId, update, cancellationToken, messageText, botClient);
            }
        }
        public async Task TikTokDownloaderReserveAPI2(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                string url = await DeleteNotUrl(messageText);

                var ytdl = new YoutubeDL();
                ytdl.YoutubeDLPath = jsonObjectAPI["ffmpegPath"][1].ToString();
                ytdl.FFmpegPath = jsonObjectAPI["ffmpegPath"][0].ToString();

                string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                if (!Directory.Exists(audioPath))
                {
                    Directory.CreateDirectory(audioPath);
                }
                string pornPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "VIDEO.MPEG4");
                string thumbnailPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVIDEO.jpeg");


                ytdl.OutputFileTemplate = pornPath;

                var res = await ytdl.RunVideoDataFetch(url);
                string title = res.Data.Title;
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                if (title.Length > 800)
                {
                    title = title.Substring(0, 800) + "...";
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
                        caption: await ads.ShowAds() + title,
                        disableNotification: false,
                        duration: Convert.ToInt32(duration),
                        parseMode: ParseMode.Html,
                        replyParameters: update.Message.MessageId
                    );

                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API 2");
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
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)",
                            replyParameters: update.Message.MessageId);
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

                await addDB.AddBotCommands(chatId, "tiktok", DateTime.Now.ToShortDateString());
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
                        replyParameters: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, цей контент недоступний або прихований для мене\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyParameters: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, данный контент недоступен или скрыт для меня\n" +
                        "\nЕсли вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyParameters: update.Message.MessageId);
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
        public async Task<string> GetVideoId(string url)
        {
            string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string finalUrl = response.RequestMessage.RequestUri.ToString();
            Regex regex = new Regex(@"(?:video|photo)/(\d+)");

            // Находим совпадение в URL
            Match match = regex.Match(finalUrl);

            if (match.Success)
            {
                // Извлекаем группу, содержащую цифры после 'video/'
                return match.Groups[1].Value;
            }
            else
            {
                // В случае отсутствия совпадений возвращаем пустую строку
                return string.Empty;
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
        private static string MakeValidFileName(string name)
        {
            // Список допустимых символов: буквы латиницы, цифры, пробел, дефис, подчеркивание, точка
            string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ .";

            // Фильтруем строку, оставляя только допустимые символы
            var cleanName = new string(name.Where(c => validChars.Contains(c)).ToArray());

            // Если после фильтрации имя пустое или слишком короткое, присваиваем стандартное имя
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                cleanName = DateTime.Now.Millisecond.ToString() + "audio"; // Стандартное имя файла
            }

            return cleanName;
        }
    }
}
