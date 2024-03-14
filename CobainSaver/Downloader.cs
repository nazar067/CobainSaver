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

namespace CobainSaver
{
    internal class Downloader
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        static string proxyURL = jsonObjectAPI["Proxy"][0].ToString();
        static string proxyUsername = jsonObjectAPI["Proxy"][1].ToString();
        static string proxyPassword = jsonObjectAPI["Proxy"][2].ToString();

        static WebProxy webProxy = new WebProxy
        {
            Address = new Uri(proxyURL),
            // specify the proxy credentials
            Credentials = new NetworkCredential(
                userName: proxyUsername,
                password: proxyPassword
          )
        };
        private static readonly HttpClient client = new HttpClient();
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = webProxy,
            UseCookies = false
        };
        private static readonly HttpClient redditClient = new HttpClient(handler);
        private static readonly HttpClient reserveClient = new HttpClient();
        private static readonly HttpClient urlClient = new HttpClient(handler);
        public Downloader()
        {
            redditClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
            reserveClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
            urlClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");
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
                Console.WriteLine(ex.ToString());
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
                startInfo.Arguments = $"\"-reconnect\" \"1\" \"-reconnect_streamed\" \"1\" \"-reconnect_delay_max\" \"5\" -c:a aac -strict experimental \"-loglevel\" \"warning\" -i \"{video}\" -i \"{audio}\" \"{result}\"";
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
        public async Task InstagramDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            /*            string link = await DeleteNotUrl(messageText);
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
                        var response = await client.GetAsync(url);
                        var responseString = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(responseString);
                        List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();

                        string text = null;
                        string currentDirectory = Directory.GetCurrentDirectory();

                        string serverFolderName = "ServerLogs";
                        string serverFolderPath = Path.Combine(currentDirectory, serverFolderName);

                        string allServers = "apiInst.txt";
                        string allFilePath = Path.Combine(serverFolderPath, allServers);
                        if (!System.IO.File.Exists(allFilePath))
                        {
                            System.IO.File.WriteAllText(allFilePath, jsonObject.ToString());
                        }
                        else
                        {
                            System.IO.File.AppendAllText(allFilePath, jsonObject.ToString());
                        }
                        if (jsonObject["graphql"]["shortcode_media"]["edge_media_to_caption"]["edges"][0]["node"]["text"] != null)
                        {
                            text = jsonObject["graphql"]["shortcode_media"]["edge_media_to_caption"]["edges"][0]["node"]["text"].ToString();
                        }

                        if (jsonObject["items"][0]["carousel_media"] != null)
                        {
                            foreach (var item in jsonObject["items"][0]["carousel_media"])
                            {
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
                                            new InputMediaVideo(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
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
                                            new InputMediaVideo(InputFile.FromUri(item["image_versions2"]["candidates"][0]["url"].ToString()))
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
                        }*/
            try
            {
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
                using (var response = await urlClient.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var rest = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var data = new Dictionary<string, string>();

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
                                                mediaAlbum.Add(
                                                    new InputMediaVideo(InputFile.FromUri(album["media"].ToString()))
                                                    {
                                                    }
                                                );
                                            }
                                            if (album["Type"].ToString() == "Image")
                                            {
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
                        text: "Sorry, post or video not found or content is private",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, пост або відео не знайдено або контент є приватним",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, пост или видео не найден или контент является приватным",
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
            Regex regex = new Regex(@"\bhttps://\S+\b");

            // Находим первое совпадение
            Match match = regex.Match(message);

            // Если найдено совпадение, возвращаем найденный URL
            return match.Value;

        }
    }
}
