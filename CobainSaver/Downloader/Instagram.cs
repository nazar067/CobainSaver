using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
using CobainSaver.DataBase;

namespace CobainSaver.Downloader
{
    internal class Instagram
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        static string torProxyUrl = jsonObjectAPI["Proxy"][3].ToString();
        public async Task InstagramStoryDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

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
                string cookie = "\"LDC\\05465822060876\\0541744405077:01f79c3838416686e332961735c89c15d00902fc70c50f603b93cd813c37c618a94322b8\"";
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 105.0.0.11.118 (iPhone11,8; iOS 12_3_1; en_US; en-US; scale=2.00; 828x1792; 165586599)");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", $"csrftoken=KbC9aLslZcCStO8VWkQvmG6xrKGqPn9a; mid=Zf_2bAALAAH_Pp3BIfcPJNiJ2Qqp; ig_did=89D16B14-549A-483B-8CEC-8ABBED73D29F; datr=qRkAZhG5zemtpjDVj3HH2Fuw; ig_nrcb=1; ds_user_id=65822060876; sessionid=65822060876:agcr4lVG6hFgVP:11:AYceChwTC8-9DWSyhFjkPc4VcBzuCntHBG7mEYpwYg; ps_n=0; ps_l=0; rur={cookie}");
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
                if (jsonObjectName["data"]["user"]["id"] != null)
                {
                    userId = jsonObjectName["data"]["user"]["id"].ToString();
                }

                var response = await instaClient.GetAsync($"https://i.instagram.com/api/v1/feed/user/{userId}/reel_media");
                var responseString = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseString);
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
                                await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
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
                                await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
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
                                "\nIf you're sure the content is available or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, термін історії закінчився\n" +
                                "\nЯкщо ви впевнені, що контент доступний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                                replyToMessageId: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, срок истории истек\n" +
                                "\nЕсли вы уверенны, что контент доступен или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, e.ToString());
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
                AddToDataBase addDB = new AddToDataBase();

                //create new httpclient
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
                string cookie = "\"LDC\\05465822060876\\0541744405077:01f79c3838416686e332961735c89c15d00902fc70c50f603b93cd813c37c618a94322b8\"";
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 105.0.0.11.118 (iPhone11,8; iOS 12_3_1; en_US; en-US; scale=2.00; 828x1792; 165586599)");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", $"csrftoken=KbC9aLslZcCStO8VWkQvmG6xrKGqPn9a; mid=Zf_2bAALAAH_Pp3BIfcPJNiJ2Qqp; ig_did=89D16B14-549A-483B-8CEC-8ABBED73D29F; datr=qRkAZhG5zemtpjDVj3HH2Fuw; ig_nrcb=1; ds_user_id=65822060876; sessionid=65822060876:agcr4lVG6hFgVP:11:AYceChwTC8-9DWSyhFjkPc4VcBzuCntHBG7mEYpwYg; ps_n=0; ps_l=0; rur={cookie}");
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
                        if (text.Length > 1024)
                        {
                            text = text.Substring(0, 1020) + "...";
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
                            await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
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
                        await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
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
                        await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                }
                await InstagramDownloaderReserve(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);

            }
        }
        public async Task InstagramDownloaderReserve(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

                await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
                //create new httpclient
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
                string cookie = "\"LDC\\05465822060876\\0541744405077:01f79c3838416686e332961735c89c15d00902fc70c50f603b93cd813c37c618a94322b8\"";
                HttpClient instaClient = new HttpClient(instaHandler);
                instaClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 105.0.0.11.118 (iPhone11,8; iOS 12_3_1; en_US; en-US; scale=2.00; 828x1792; 165586599)");
                instaClient.DefaultRequestHeaders.Add("Host", "www.instagram.com");
                instaClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                instaClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.8,uk;q=0.6,en-US;q=0.4,en;q=0.2");
                instaClient.DefaultRequestHeaders.Add("Cookie", $"csrftoken=KbC9aLslZcCStO8VWkQvmG6xrKGqPn9a; mid=Zf_2bAALAAH_Pp3BIfcPJNiJ2Qqp; ig_did=89D16B14-549A-483B-8CEC-8ABBED73D29F; datr=qRkAZhG5zemtpjDVj3HH2Fuw; ig_nrcb=1; ds_user_id=65822060876; sessionid=65822060876:agcr4lVG6hFgVP:11:AYceChwTC8-9DWSyhFjkPc4VcBzuCntHBG7mEYpwYg; ps_n=0; ps_l=0; rur={cookie}");
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
                        if (text.Length > 1024)
                        {
                            text = text.Substring(0, 1020) + "...";
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
                            await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
                            var message = update.Message;
                            var user = message.From;
                            var chat = message.Chat;
                            Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                            await logs.WriteServerLogs();
                        }
                    }
                    else if (jsonObject["items"][0]["video_versions"] != null)
                    {
                        await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                        string video = jsonObject["items"][0]["video_versions"][0]["url"].ToString();
                        string thumbnailVideo = jsonObject["items"][0]["image_versions2"]["candidates"][0]["url"].ToString();
                        double duration = Convert.ToDouble(jsonObject["items"][0]["video_duration"]);
                        int roundedDuration = (int)Math.Round(duration);

                        string audioPath = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\audio";

                        string videoPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "video.MPEG4");
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

                        try
                        {
                            await botClient.SendVideoAsync(
                                chatId: chatId,
                                video: InputFile.FromStream(streamVideo),
                                thumbnail: InputFile.FromStream(streamThumbVideo),
                                duration: roundedDuration,
                                caption: text,
                                replyToMessageId: update.Message.MessageId);
                            await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
                            var message = update.Message;
                            var user = message.From;
                            var chat = message.Chat;
                            Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                            await logs.WriteServerLogs();
                        }
                        catch (Exception ex)
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
                            await InstagramDownloaderReserveAPI(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);
                        }

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

                        string imgPath = Path.Combine(audioPath, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");

                        using (var client = new WebClient())
                        {
                            client.DownloadFile(img, imgPath);
                        }
                        await using Stream streamImg = System.IO.File.OpenRead(imgPath);

                        try
                        {
                            await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: InputFile.FromStream(streamImg),
                                caption: text,
                                replyToMessageId: update.Message.MessageId);
                            await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
                            var message = update.Message;
                            var user = message.From;
                            var chat = message.Chat;
                            Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent");
                            await logs.WriteServerLogs();
                        }
                        catch (Exception ex)
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
                            await InstagramDownloaderReserveAPI(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);
                        }

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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                }
                await InstagramDownloaderReserveAPI(chatId, update, cancellationToken, messageText, (TelegramBotClient)botClient);

            }
        }
        public async Task InstagramDownloaderReserveAPI(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();

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
                //await Console.Out.WriteLineAsync("after request");
                using (var response = await instaClient.SendAsync(request))
                {
                    //await Console.Out.WriteLineAsync("after response");
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
                    await addDB.AddBotCommands(chatId, "insta", DateTime.Now.ToShortDateString());
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, "OK, content has been sent by reserve API");
                    await logs.WriteServerLogs();
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
                        "\nIf you're sure the content is public or the bot has previously submitted this, please write us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, пост або відео не знайдено або контент є приватним\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, пост или видео не найден или контент является приватным\n" +
                        "\nЕсли вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                try
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, messageText, e.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception ex)
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
