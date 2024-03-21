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
using Newtonsoft.Json;
using System.Net.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Numerics;
using AngleSharp.Browser;
using YoutubeDLSharp.Metadata;

namespace CobainSaver
{
    internal class Downloader
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
/*        static WebProxy torProxy = new WebProxy
        {
            Address = new Uri(torProxyUrl),
        };*/
        private static readonly HttpClient client = new HttpClient();
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = webProxy,
            UseCookies = false
        };
/*        private static HttpClientHandler instaHandler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = torProxy,
            UseCookies = false
        };*/
        private static readonly HttpClient redditClient = new HttpClient(handler);
        private static readonly HttpClient reserveClient = new HttpClient();
        private static readonly HttpClient urlClient = new HttpClient(handler);
        //private static readonly HttpClient instaClient = new HttpClient(instaHandler);
        public Downloader()
        {
            redditClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
            reserveClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
            urlClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
            //instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
        }
        public async Task YoutubeDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            Stream stream = null;
            //превью видео
            try
            {
                var youtube = new YoutubeClient();
                var allInfo = await youtube.Videos.GetAsync(normallMsg);

                var videoUrl = normallMsg;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                string size = streamInfo.Size.MegaBytes.ToString();
                if(Convert.ToDouble(size) >= 50)
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
                            text: "Вибачте, це відео має проблему: відео занадто велике (розмір має не перевищувати 50мб)",
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

                // Отправляем видео обратно пользователю
                await botClient.SendVideoAsync(
                    chatId: chatId,
                    caption: title,
                    video: InputFile.FromStream(streamVideo),
                    thumbnail: InputFile.FromStream(streamThumbVideo),
                    duration: Convert.ToInt32(duration),
                    replyToMessageId: update.Message.MessageId);
                streamVideo.Close();
                streamThumbVideo.Close();
                System.IO.File.Delete(videoPath);
                System.IO.File.Delete(thumbnailVideoPath);
            }
            catch (Exception ex)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if(lang == "eng")
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
                        text: "Вибачте, це відео має проблему: відео має вікові обмеження",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, с этим видео возникли проблемы: видео имеет возрастное ограничение",
                        replyToMessageId: update.Message.MessageId);
                }
            //throw;
            }
            finally
            {
                    
            }
        }
        public async Task YoutubeMusicDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string normallMsg = await DeleteNotUrl(messageText);
            Stream stream = null;
            //превью видео
            try
            {
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
                            text: "Вибачте, це аудіо має проблему: аудіо занадто велике (розмір має не перевищувати 50мб)",
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
                catch(Exception e)
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
                await botClient.SendAudioAsync(
                    chatId: chatId,
                    title: title,
                    audio: InputFile.FromStream(streamAudio),
                    performer: author,
                    thumbnail: InputFile.FromStream(streamThumbAudio),
                    duration: Convert.ToInt32(duration),
                    replyToMessageId: update.Message.MessageId); ;
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
                //throw;
            }
            finally
            {

            }
        }
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
                long size = Convert.ToInt64(jsonObject["data"]["size"].ToString());
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
                            text: "Вибачте, це відео має проблему: відео занадто велике (розмір має не перевищувати 50мб)",
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
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
            }
            catch(Exception ex) 
            {
                if(ex.Message.Contains("Bad Request: failed to get HTTP URL content"))
                {
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

                    string videoPath = Path.Combine(audioPath, chatId + "video.mp4");
                    string thumbnailVideo = jsonObject["data"]["origin_cover"].ToString();
                    string thumbnailVideoPath = Path.Combine(audioPath, chatId + "thumbVideo.jpeg");

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
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromStream(streamVideo),
                        caption: title,
                        disableNotification: true,
                        duration: videoDuration,
                        thumbnail: InputFile.FromStream(streamThumbVideo),
                        replyToMessageId: update.Message.MessageId
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
                        text: "Sorry, this content is not available or hidden to me\n" +
                        "If you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, цей контент недоступний або прихований для мене\n" +
                        "Якщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, данный контент недоступен или скрыт для меня\n" +
                        "Если вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
/*        public async Task TikTokDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                string normallMsg = await DeleteNotUrl(messageText);
                var response = await urlClient.GetAsync(normallMsg);
                var responseString = await response.Content.ReadAsStringAsync();
                Regex regex = new Regex(@"\bwebapp.video-detail\S+\b");
                Match match = regex.Match(responseString);
                string id = null;
                if (response.StatusCode.ToString() == "MovedPermanently")
                {
                    string[] parts = responseString.Split(new char[] { '/', '?' });

                    // Выбираем последнюю подстроку
                    id = parts[parts.Length - 3];
                }
                else if (!match.Success)
                {
                    try
                    {
                        regex = new Regex(@"\bseo.abtest\S+\b");
                        match = regex.Match(responseString);
                        id = match.Value.Split('F').Last();
                        id = id.Remove(id.IndexOf(','));
                        id = id.Substring(0, (id.Length - 1));
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else if (match.Success)
                {
                    try
                    {
                        id = match.Value.Substring(54);
                        id = id.Split('"')[0];
                    }
                    catch (Exception ex)
                    {
                        id = normallMsg.Split('/').Last();
                        id = id.Remove(id.IndexOf('?'));
                    }
                }

                string url = $"{jsonObjectAPI["TTAPI"][1]}{id}";
                var responseVideo = await reserveClient.GetAsync(url);
                var responseStringVideo = await responseVideo.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseStringVideo);


                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                if (jsonObject["aweme_list"][0]["image_post_info"] != null)
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                    int count = 0;
                    foreach (var album in jsonObject["aweme_list"][0]["image_post_info"]["images"])
                    {
                        foreach (var media in album["display_image"]["url_list"])
                        {
                            if (count % 2 == 0)
                            {
                                mediaAlbum.Add(
                                     new InputMediaPhoto(InputFile.FromUri(media.ToString()))
                                );
                            }
                            count++;
                        }
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
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["url_list"][0].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string title = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();

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
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream),
                        performer: perfomer,
                        title: title,
                        duration: duration,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyToMessageId: update.Message.MessageId
                        );
                    stream.Close();
                    streamThumb.Close();

                    System.IO.File.Delete(filePath);
                    System.IO.File.Delete(thumbnailPath);
                }
                else
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                    string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                    if (!Directory.Exists(audioPath))
                    {
                        Directory.CreateDirectory(audioPath);
                    }
                    string video = jsonObject["aweme_list"][0]["video"]["download_addr"]["url_list"][0].ToString();
                    string title = jsonObject["aweme_list"][0]["desc"].ToString();
                    if (title.Contains("#"))
                    {
                        title = Regex.Replace(title, @"#.*", "");
                    }
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromUri(video),
                        caption: title,
                        replyToMessageId: update.Message.MessageId
                        );
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    string music = jsonObject["aweme_list"][0]["music"]["play_url"]["url_list"][0].ToString();
                    string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                    string musicTitle = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                    int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                    string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();

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
                    await botClient.SendAudioAsync(
                        chatId: chatId,
                        audio: InputFile.FromStream(stream),
                        performer: perfomer,
                        title: musicTitle,
                        duration: duration,
                        thumbnail: InputFile.FromStream(streamThumb),
                        replyToMessageId: update.Message.MessageId
                        );
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
                    string jsonString = System.IO.File.ReadAllText("source.json");
                    JObject jsonObjectAPI = JObject.Parse(jsonString);
                    string normallMsg = await DeleteNotUrl(messageText);
                    var response = await urlClient.GetAsync(normallMsg);
                    var responseString = await response.Content.ReadAsStringAsync();
                    Regex regex = new Regex(@"\bwebapp.video-detail\S+\b");
                    Match match = regex.Match(responseString);
                    string id = null;
                    if(response.StatusCode.ToString() == "MovedPermanently")
                    {
                        string[] parts = responseString.Split(new char[] { '/', '?' });

                        // Выбираем последнюю подстроку
                        id = parts[parts.Length - 3];
                    }
                    else if (!match.Success)
                    {
                        try
                        {
                            regex = new Regex(@"\bseo.abtest\S+\b");
                            match = regex.Match(responseString);
                            id = match.Value.Split('F').Last();
                            id = id.Remove(id.IndexOf(','));
                            id = id.Substring(0, (id.Length - 1));
                        }
                        catch (Exception e)
                        {

                        }
                    }
                    else if (match.Success)
                    {
                        try
                        {
                            id = match.Value.Substring(54);
                            id = id.Split('"')[0];
                        }
                        catch (Exception e)
                        {
                            id = normallMsg.Split('/').Last();
                            id = id.Remove(id.IndexOf('?'));
                        }
                    }
                    string url = $"{jsonObjectAPI["TTAPI"][1]}{id}";
                    var responseVideo = await reserveClient.GetAsync(url);
                    var responseStringVideo = await responseVideo.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseStringVideo);


                    List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
                    if (jsonObject["aweme_list"][0]["image_post_info"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                        int count = 0;
                        foreach (var album in jsonObject["aweme_list"][0]["image_post_info"]["images"])
                        {
                            foreach (var media in album["display_image"]["url_list"])
                            {
                                if (count % 2 == 0)
                                {
                                    mediaAlbum.Add(
                                         new InputMediaPhoto(InputFile.FromUri(media.ToString()))
                                    );
                                }
                                count++;
                            }
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
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                        string music = jsonObject["aweme_list"][0]["music"]["play_url"]["url_list"][0].ToString();
                        string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                        string title = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                        int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                        string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();

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
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: title,
                            duration: duration,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
                        stream.Close();
                        streamThumb.Close();

                        System.IO.File.Delete(filePath);
                        System.IO.File.Delete(thumbnailPath);
                    }
                    else
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                        string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";
                        if (!Directory.Exists(audioPath))
                        {
                            Directory.CreateDirectory(audioPath);
                        }
                        string video = jsonObject["aweme_list"][0]["video"]["download_addr"]["url_list"][0].ToString();
                        string videoPath = Path.Combine(audioPath, chatId + "video.mp4");
                        string thumbnailVideo = jsonObject["aweme_list"][0]["video"]["origin_cover"]["url_list"][0].ToString();
                        string thumbnailVideoPath = Path.Combine(audioPath, chatId + "thumbVideo.jpeg");
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

                        string title = jsonObject["aweme_list"][0]["desc"].ToString();
                        if (title.Contains("#"))
                        {
                            title = Regex.Replace(title, @"#.*", "");
                        }
                        await botClient.SendVideoAsync(
                            chatId: chatId,
                            video: InputFile.FromStream(streamVideo),
                            caption: title,
                            thumbnail: InputFile.FromStream(streamThumbVideo),
                            replyToMessageId: update.Message.MessageId
                            );
                        streamVideo.Close();
                        streamThumbVideo.Close();
                        System.IO.File.Delete(videoPath);
                        System.IO.File.Delete(thumbnailVideoPath);
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                        string music = jsonObject["aweme_list"][0]["music"]["play_url"]["url_list"][0].ToString();
                        string perfomer = jsonObject["aweme_list"][0]["music"]["author"].ToString();
                        string musicTitle = jsonObject["aweme_list"][0]["music"]["title"].ToString();
                        int duration = Convert.ToInt32(jsonObject["aweme_list"][0]["music"]["duration"]);
                        string thumbnail = jsonObject["aweme_list"][0]["music"]["cover_thumb"]["url_list"][0].ToString();

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
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: InputFile.FromStream(stream),
                            performer: perfomer,
                            title: musicTitle,
                            duration: duration,
                            thumbnail: InputFile.FromStream(streamThumb),
                            replyToMessageId: update.Message.MessageId
                            );
                        stream.Close();
                        streamThumb.Close();

                        System.IO.File.Delete(filePath);
                        System.IO.File.Delete(thumbnailPath);
                    }
                }
                catch
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this content is not available or hidden to me",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей контент недоступний або прихований для мене",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, данный контент недоступен или скрыт для меня",
                            replyToMessageId: update.Message.MessageId);
                    }
                    try
                    {
                        var message = update.Message;
                        var user = message.From;
                        var chat = message.Chat;
                        Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                        await logs.WriteServerLogs();
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }
                
            }
        }*/
        public async Task ReditDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);
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
        public async Task TwitterDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);
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
            var response = await client.GetAsync(jsonObjectAPI["XAPI"][0] + mediaId);
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
        public async Task InstagramStoryDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                WebProxy torProxy = new WebProxy
                {
                    Address = new Uri(torProxyUrl),
                };
                HttpClientHandler instaHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    Proxy = torProxy,
                    UseCookies = false
                };
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 105.0.0.11.118 (iPhone11,8; iOS 12_3_1; en_US; en-US; scale=2.00; 828x1792; 165586599)");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", "csrftoken=4iieZCxBIaLbEKX4HSdGW99e1C28Jss5; mid=Zau1bgALAAEAQ8kuhhpz2ivZF4U8; ig_did=572E90C4-8466-40C9-8A51-A42EF1C350A6; datr=brWrZQvqYG8A_ZQcVaFYOt7s; ig_nrcb=1; ds_user_id=53733646477; sessionid=53733646477%3A2tCC8w9we5kcvQ%3A9%3AAYe50kiNdAjeb7dvXd-zvFFxCLXcFMJZ_drKKC6slw; ps_n=0; ps_l=0; rur=\"ODN\\05453733646477\\0541742467210:01f7429eedb553317ff7b6f9c93668ff8bdd0c006928385ba0f709c77e60adb4f5dae57a");
                instaClient.DefaultRequestHeaders.Add("Accept-Encoding", "Accept-Encoding");
                instaClient.DefaultRequestHeaders.Add("Alt-Used", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                instaClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
                instaClient.DefaultRequestHeaders.Add("TE", "trailers");

                string link = await DeleteNotUrl(messageText);
                string userName = null;
                string userId = null;
                string storyId = null;

                string pattern = @"stories/(?<username>[^/]+)/";
                Regex regex = new Regex(pattern);

                Match match = regex.Match(link);

                if (match.Success)
                {
                    userName = match.Groups["username"].Value;
                }

                int questionMarkIndex = link.IndexOf('?');
                if (questionMarkIndex != -1)
                {
                    link = link.Substring(0, questionMarkIndex);
                }
                int lastIndex = link.LastIndexOf('/');
                storyId = link.Substring(lastIndex + 1);

                var responseName = await instaClient.GetAsync($"https://i.instagram.com/api/v1/users/web_profile_info/?username={userName}");
                var responseStringName = await responseName.Content.ReadAsStringAsync();
                JObject jsonObjectName = JObject.Parse(responseStringName);
                //Console.WriteLine(jsonObjectName.ToString());
                if (jsonObjectName["data"]["user"]["id"] != null)
                {
                    userId = jsonObjectName["data"]["user"]["id"].ToString();
                }

                var response = await instaClient.GetAsync($"https://i.instagram.com/api/v1/feed/user/{userId}/reel_media");
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);
                //Console.WriteLine(jsonObject.ToString());
                int count = 0;
                if (jsonObject["items"] != null)
                {
                    foreach (var item in jsonObject["items"])
                    {
                        if (storyId == item["pk"].ToString())
                        {
                            if (item["video_versions"] != null)
                            {
                                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                                string video = item["video_versions"][0]["url"].ToString();
                                await botClient.SendVideoAsync(
                                    chatId: chatId,
                                    video: InputFile.FromUri(video),
                                    replyToMessageId: update.Message.MessageId);
                                count++;
                            }
                            else
                            {
                                await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                                string photo = item["image_versions2"]["candidates"][0]["url"].ToString();
                                await botClient.SendPhotoAsync(
                                    chatId: chatId,
                                    photo: InputFile.FromUri(photo),
                                    replyToMessageId: update.Message.MessageId);
                                count++;
                            }
                        }
                    }
                    if (count == 0)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                        Language language = new Language("rand", "rand");
                        string lang = await language.GetCurrentLanguage(chatId.ToString());
                        if (lang == "eng")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Sorry, story's expired\n" +
                                "If you're sure the content is available or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, термін історії закінчився\n" +
                                "Якщо ви впевнені, що контент доступний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, срок истории истек\n" +
                                "Если вы уверенны, что контент доступен или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                                replyToMessageId: update.Message.MessageId);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, story not found or content is private\n" +
                        "If you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, сторі не знайдено або контент є приватним\n" +
                        "Якщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, история не найдена или контент является приватным\n" +
                        "Если вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, e.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }


        public async Task InstagramDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                //create new httpclient
                WebProxy torProxy = new WebProxy
                {
                    Address = new Uri(torProxyUrl),
                };
                HttpClientHandler instaHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    Proxy = torProxy,
                    UseCookies = false
                };
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", "csrftoken=4iieZCxBIaLbEKX4HSdGW99e1C28Jss5; mid=Zau1bgALAAEAQ8kuhhpz2ivZF4U8; ig_did=572E90C4-8466-40C9-8A51-A42EF1C350A6; datr=brWrZQvqYG8A_ZQcVaFYOt7s; ig_nrcb=1; ds_user_id=53733646477; sessionid=53733646477%3A2tCC8w9we5kcvQ%3A9%3AAYe50kiNdAjeb7dvXd-zvFFxCLXcFMJZ_drKKC6slw; ps_n=0; ps_l=0; rur=\"ODN\\05453733646477\\0541742467210:01f7429eedb553317ff7b6f9c93668ff8bdd0c006928385ba0f709c77e60adb4f5dae57a");
                instaClient.DefaultRequestHeaders.Add("Accept-Encoding", "Accept-Encoding");
                instaClient.DefaultRequestHeaders.Add("Alt-Used", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                instaClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
                instaClient.DefaultRequestHeaders.Add("TE", "trailers");
                //

                string link = await DeleteNotUrl(messageText);
                if (link.Contains("/stories/"))
                {
                    await InstagramStoryDownloader(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);
                }
                else
                {
                    string id = null;
                    if (link.Contains("/p/"))
                    {
                        string pattern = @"\/p\/([^\s\/?]+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(link);

                        if (match.Success)
                        {
                            id = match.Groups[1].Value;
                        }
                    }
                    else if (link.Contains("/reels/") || link.Contains("/reel/"))
                    {
                        string pattern = @"\/reels\/([^\s\/?]+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(link);

                        if (match.Success)
                        {
                            id = match.Groups[1].Value;
                        }
                        else
                        {
                            pattern = @"\/reel\/([^\s\/?]+)";
                            regex = new Regex(pattern);
                            match = regex.Match(link);
                            if (match.Success)
                            {
                                id = match.Groups[1].Value;
                            }
                        }
                    }
                    int count = 0;
                    string url = "https://www.instagram.com/p/" + id + "/?__a=1&__d=dis";
                    var response = await instaClient.GetAsync(url);
                    var responseString = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseString);
                    List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();

                    string text = null;
                    try
                    {
                        if (jsonObject["items"][0]["caption"]["text"] != null)
                        {
                            text = jsonObject["items"][0]["caption"]["text"].ToString();
                        }
                        if (text.Contains("#"))
                        {
                            text = Regex.Replace(text, @"#.*", "");
                        }
                    }
                    catch
                    {
                    }
                    if (jsonObject["items"][0]["carousel_media_ids"] != null)
                    {
                        foreach (var item in jsonObject["items"][0]["carousel_media"])
                        {
                            await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                            if (count == 0)
                            {
                                if (item["video_versions"] != null)
                                {
                                    mediaAlbum.Add(
                                        new InputMediaVideo(InputFile.FromUri(item["video_versions"][0]["url"].ToString()))
                                        {
                                            Caption = text,
                                        }
                                    );
                                }
                                else
                                {
                                    mediaAlbum.Add(
                                        new InputMediaPhoto(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
                                        {
                                            Caption = text,
                                        }
                                    );
                                }
                            }
                            else
                            {
                                if (item["video_versions"] != null)
                                {
                                    mediaAlbum.Add(
                                        new InputMediaVideo(InputFile.FromUri(item["video_versions"][0]["url"].ToString()))
                                        {
                                        }
                                    );
                                }
                                else
                                {
                                    mediaAlbum.Add(
                                        new InputMediaPhoto(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
                                        {
                                        }
                                    );
                                }
                            }
                            count++;
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
                    else if (jsonObject["items"][0]["video_versions"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                        string video = jsonObject["items"][0]["video_versions"][0]["url"].ToString();
                        await botClient.SendVideoAsync(
                            chatId: chatId,
                            video: InputFile.FromUri(video),
                            caption: text,
                            replyToMessageId: update.Message.MessageId);
                    }
                    else if (jsonObject["items"][0]["image_versions2"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                        string img = jsonObject["items"][0]["image_versions2"]["candidates"][0]["url"].ToString();
                        await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: InputFile.FromUri(img),
                            caption: text,
                            replyToMessageId: update.Message.MessageId);
                    }
                }
                
            }
            catch (Exception ex)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
                await InstagramDownloaderReserve(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);

            }
        }
        public async Task InstagramDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                //create new httpclient
                WebProxy torProxy = new WebProxy
                {
                    Address = new Uri(torProxyUrl),
                };
                HttpClientHandler instaHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    Proxy = torProxy,
                    UseCookies = false
                };
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", "csrftoken=4iieZCxBIaLbEKX4HSdGW99e1C28Jss5; mid=Zau1bgALAAEAQ8kuhhpz2ivZF4U8; ig_did=572E90C4-8466-40C9-8A51-A42EF1C350A6; datr=brWrZQvqYG8A_ZQcVaFYOt7s; ig_nrcb=1; ds_user_id=53733646477; sessionid=53733646477%3A2tCC8w9we5kcvQ%3A9%3AAYe50kiNdAjeb7dvXd-zvFFxCLXcFMJZ_drKKC6slw; ps_n=0; ps_l=0; rur=\"ODN\\05453733646477\\0541742467210:01f7429eedb553317ff7b6f9c93668ff8bdd0c006928385ba0f709c77e60adb4f5dae57a");
                instaClient.DefaultRequestHeaders.Add("Accept-Encoding", "Accept-Encoding");
                instaClient.DefaultRequestHeaders.Add("Alt-Used", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                instaClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                instaClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
                instaClient.DefaultRequestHeaders.Add("TE", "trailers");
                //

                string link = await DeleteNotUrl(messageText);
                if (link.Contains("/stories/"))
                {
                    await InstagramStoryDownloader(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);
                }
                else
                {
                    string id = null;
                    if (link.Contains("/p/"))
                    {
                        string pattern = @"\/p\/([^\s\/?]+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(link);

                        if (match.Success)
                        {
                            id = match.Groups[1].Value;
                        }
                    }
                    else if (link.Contains("/reels/") || link.Contains("/reel/"))
                    {
                        string pattern = @"\/reels\/([^\s\/?]+)";
                        Regex regex = new Regex(pattern);
                        Match match = regex.Match(link);

                        if (match.Success)
                        {
                            id = match.Groups[1].Value;
                        }
                        else
                        {
                            pattern = @"\/reel\/([^\s\/?]+)";
                            regex = new Regex(pattern);
                            match = regex.Match(link);
                            if (match.Success)
                            {
                                id = match.Groups[1].Value;
                            }
                        }
                    }
                    int count = 0;
                    string url = "https://www.instagram.com/p/" + id + "/?__a=1&__d=dis";
                    var response = await instaClient.GetAsync(url);
                    var responseString = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseString);
                    List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();

                    string text = null;
                    try
                    {
                        if (jsonObject["items"][0]["caption"]["text"] != null)
                        {
                            text = jsonObject["items"][0]["caption"]["text"].ToString();
                        }
                        if (text.Contains("#"))
                        {
                            text = Regex.Replace(text, @"#.*", "");
                        }
                    }
                    catch
                    {
                    }
                    if (jsonObject["items"][0]["carousel_media_ids"] != null)
                    {
                        foreach (var item in jsonObject["items"][0]["carousel_media"])
                        {
                            await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                            if (count == 0)
                            {
                                if (item["video_versions"] != null)
                                {
                                    mediaAlbum.Add(
                                        new InputMediaVideo(InputFile.FromUri(item["video_versions"][0]["url"].ToString()))
                                        {
                                            Caption = text,
                                        }
                                    );
                                }
                                else
                                {
                                    mediaAlbum.Add(
                                        new InputMediaPhoto(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
                                        {
                                            Caption = text,
                                        }
                                    );
                                }
                            }
                            else
                            {
                                if (item["video_versions"] != null)
                                {
                                    mediaAlbum.Add(
                                        new InputMediaVideo(InputFile.FromUri(item["video_versions"][0]["url"].ToString()))
                                        {
                                        }
                                    );
                                }
                                else
                                {
                                    mediaAlbum.Add(
                                        new InputMediaPhoto(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
                                        {
                                        }
                                    );
                                }
                            }
                            count++;
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
                    else if (jsonObject["items"][0]["video_versions"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                        string video = jsonObject["items"][0]["video_versions"][0]["url"].ToString();
                        string thumbnailVideo = jsonObject["items"][0]["image_versions2"]["candidates"][0]["url"].ToString();

                        string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";

                        string videoPath = Path.Combine(audioPath, chatId + "video.mp4");
                        string thumbnailVideoPath = Path.Combine(audioPath, chatId + "thumbVideo.jpeg");

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

                        await botClient.SendVideoAsync(
                            chatId: chatId,
                            video: InputFile.FromStream(streamVideo),
                            thumbnail: InputFile.FromStream(streamThumbVideo),
                            caption: text,
                            replyToMessageId: update.Message.MessageId);

                        streamVideo.Close();
                        streamThumbVideo.Close();
                        System.IO.File.Delete(videoPath);
                        System.IO.File.Delete(thumbnailVideoPath);
                    }
                    else if (jsonObject["items"][0]["image_versions2"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                        string img = jsonObject["items"][0]["image_versions2"]["candidates"][0]["url"].ToString();

                        string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";

                        string imgPath = Path.Combine(audioPath, chatId + "thumbVideo.jpeg");

                        using (var client = new WebClient())
                        {
                            client.DownloadFile(img, imgPath);
                        }
                        await using Stream streamImg = System.IO.File.OpenRead(imgPath);

                        await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: InputFile.FromStream(streamImg),
                            caption: text,
                            replyToMessageId: update.Message.MessageId);

                        streamImg.Close();
                        System.IO.File.Delete(imgPath);
                    }
                }

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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
                await InstagramDownloaderReserveAPI(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);

            }
        }
        public async Task InstagramDownloaderReserveAPI(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                //create new httpclient
                WebProxy torProxy = new WebProxy
                {
                    Address = new Uri(torProxyUrl),
                };
                HttpClientHandler instaHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    Proxy = torProxy,
                    UseCookies = false
                };
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
                //

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);
                int count = 0;
                List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();

                string link = await DeleteNotUrl(messageText);
                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Get,
                    RequestUri = new Uri(jsonObjectAPI["InstagramAPI"][0] + link),
                    Headers =
                    {
                        { "X-RapidAPI-Key", jsonObjectAPI["InstagramAPI"][1].ToString() },
                        { "X-RapidAPI-Host", jsonObjectAPI["InstagramAPI"][2].ToString() }
                    }
                };
                await Console.Out.WriteLineAsync("after request");
                using (var response = await instaClient.SendAsync(request))
                {
                    await Console.Out.WriteLineAsync("after response");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var rest = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var data = new Dictionary<string, string>();
                        //Console.WriteLine(rest.ToString());

                        if (rest["Type"] != null)
                        {
                            switch (rest["Type"].ToString())
                            {
                                case "Post-Video":
                                    data["media"] = rest["media"].ToString();
                                    data["type"] = "video";
                                    mediaAlbum.Add(
                                        new InputMediaVideo(InputFile.FromUri(rest["media"].ToString()))
                                        {
                                            Caption = rest["title"].ToString(),
                                        }
                                    );
                                    break;
                                case "Carousel":
                                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                                    data["media"] = rest["media_with_thumb"].ToString();
                                    data["type"] = "carousel";
                                    foreach (var album in rest["media_with_thumb"])
                                    {
                                        if (count == 0)
                                        {
                                            if (album["Type"].ToString() == "Video")
                                            {
                                                mediaAlbum.Add(
                                                    new InputMediaVideo(InputFile.FromUri(album["media"].ToString()))
                                                    {
                                                        Caption = rest["title"].ToString(),
                                                    }
                                                );
                                            }
                                            if (album["Type"].ToString() == "Image")
                                            {
                                                mediaAlbum.Add(
                                                    new InputMediaPhoto(InputFile.FromUri(album["media"].ToString()))
                                                    {
                                                        Caption = rest["title"].ToString(),
                                                    }
                                                );
                                            }
                                        }
                                        else
                                        {
                                            if (album["Type"].ToString() == "Video")
                                            {
                                                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                                                mediaAlbum.Add(
                                                    new InputMediaVideo(InputFile.FromUri(album["media"].ToString()))
                                                    {
                                                    }
                                                );
                                            }
                                            if (album["Type"].ToString() == "Image")
                                            {
                                                await botClient.SendChatActionAsync(chatId, ChatAction.UploadPhoto);
                                                mediaAlbum.Add(
                                                    new InputMediaPhoto(InputFile.FromUri(album["media"].ToString()))
                                                    {
                                                    }
                                                );
                                            }
                                        }
                                        count++;
                                    }
                                    break;
                                case "Post-Image":
                                    data["media"] = rest["media"].ToString();
                                    data["type"] = "image";
                                    mediaAlbum.Add(
                                        new InputMediaPhoto(InputFile.FromUri(rest["media"].ToString()))
                                        {
                                            Caption = rest["title"].ToString(),
                                        }
                                    );
                                    break;
                                default:
                                    data["type"] = "error";
                                    break;
                            }
                        }
                        else if (rest["story_by_id"]["Type"].ToString() == "Story-Video")
                        {
                            mediaAlbum.Add(
                                new InputMediaVideo(InputFile.FromUri(rest["story_by_id"]["media"].ToString()))
                                {
                                    Caption = rest["username"].ToString() + "'s story",
                                }
                            );
                        }
                        else if (rest["story_by_id"]["Type"].ToString() == "Story-Image")
                        {
                            mediaAlbum.Add(
                                new InputMediaPhoto(InputFile.FromUri(rest["story_by_id"]["media"].ToString()))
                                {
                                    Caption = rest["username"].ToString() + "'s story",
                                }
                            );
                        }
                    }
                    else
                    {
                        await Console.Out.WriteLineAsync(await response.Content.ReadAsStringAsync());
                        throw new Exception($"Failed to retrieve data: {response.ReasonPhrase}");
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
            catch (Exception e)
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, post or video not found or content is private\n" +
                        "If you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, пост або відео не знайдено або контент є приватним\n" +
                        "Якщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, пост или видео не найден или контент является приватным\n" +
                        "Если вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, e.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception ex)
                {
                    return;
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
