﻿using AngleSharp.Dom;
using CobainSaver.DataBase;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VideoLibrary;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

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
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

                var youtube = new YoutubeClient();
                var allInfo = await youtube.Videos.GetAsync(normallMsg);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(normallMsg);

                // Получаем видео и аудио потоки отдельно

                var videoStreams = streamManifest.GetVideoOnlyStreams();

                VideoOnlyStreamInfo videoStreamInfo = null;

                foreach (var video in videoStreams)
                {
                    if (video.VideoQuality.Label == "480p" && video.Size.MegaBytes < 35)
                    {
                        videoStreamInfo = streamManifest.GetVideoOnlyStreams()
                                             .FirstOrDefault(s => s.VideoQuality.Label == "480p");
                        break;
                    }
                    if (video.VideoQuality.Label == "360p" && video.Size.MegaBytes < 35)
                    {
                        videoStreamInfo = streamManifest.GetVideoOnlyStreams()
                                             .FirstOrDefault(s => s.VideoQuality.Label == "360p");
                        break;
                    }
                    if (video.VideoQuality.Label == "240p" && video.Size.MegaBytes < 35)
                    {
                        videoStreamInfo = streamManifest.GetVideoOnlyStreams()
                                             .FirstOrDefault(s => s.VideoQuality.Label == "240p");
                        break;
                    }
                    if (video.VideoQuality.Label == "144p" && video.Size.MegaBytes < 35)
                    {
                        videoStreamInfo = streamManifest.GetVideoOnlyStreams()
                                             .FirstOrDefault(s => s.VideoQuality.Label == "144p");
                        break;
                    }
                }

                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                string sizeVideo = videoStreamInfo.Size.MegaBytes.ToString();
                string sizeAudio = audioStreamInfo.Size.MegaBytes.ToString();
                if (Convert.ToDouble(sizeVideo) + Convert.ToDouble(sizeAudio) >= 50)
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
                //stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                string duration = allInfo.Duration.Value.TotalSeconds.ToString();
                string title = allInfo.Title;
                if (title.Contains("#"))
                {
                    title = Regex.Replace(title, @"#.*", "");
                }
                string thumbnail = "https://img.youtube.com/vi/" + allInfo.Id + "/maxresdefault.jpg";

                string path = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string videoPath = Path.Combine(path, DateTime.Now.Millisecond.ToString() + $"_video.mp4");
                string audioPath = Path.Combine(path, DateTime.Now.Millisecond.ToString() + $"_audio.m4a");
                string outputPath = Path.Combine(path, DateTime.Now.Millisecond.ToString() + $"_final.mp4");
                string thumbnailVideoPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailVideoPath);
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

                await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, videoPath);
                await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);

                var videoStream = await FFmpeg.GetMediaInfo(videoPath);
                var audioStream = await FFmpeg.GetMediaInfo(audioPath);

                await FFmpeg.Conversions.New()
                    .AddStream(videoStream.VideoStreams.First())
                    .AddStream(audioStream.AudioStreams.First())
                    .SetOutput(outputPath)
                    .Start();

                await using Stream streamVideo = System.IO.File.OpenRead(outputPath);
                await using Stream streamThumbVideo = System.IO.File.OpenRead(thumbnailVideoPath);

                try
                {
                    // Отправляем видео обратно пользователю
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        caption: await ads.ShowAds() + title,
                        video: InputFile.FromStream(streamVideo),
                        thumbnail: InputFile.FromStream(streamThumbVideo),
                        duration: Convert.ToInt32(duration),
                        parseMode: ParseMode.Html,
                        replyParameters: update.Message.MessageId);
                    await addDB.AddBotCommands(chatId, "youtube", DateTime.Now.ToShortDateString());
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
                            text: "Sorry, this video has a problem: the video has an age restriction",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим відео виникла помилка: відео має вікові обмеження",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим видео возникли проблемы: видео имеет возрастное ограничение",
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
                streamThumbVideo.Close();
                System.IO.File.Delete(videoPath);
                System.IO.File.Delete(audioPath);
                System.IO.File.Delete(outputPath);
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
                Ads ads = new Ads();

                AddToDataBase addDB = new AddToDataBase();

                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                await botClient.SendChatActionAsync(chatId, ChatAction.UploadVideo);
                string url = await DeleteNotUrl(messageText);

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
                        duration: Convert.ToInt32(duration),
                        parseMode: ParseMode.Html
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
                            text: "Sorry, this video has a problem: the video is too big (the size should not exceed 50mb)");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, з цим відео виникла помилка: відео занадто велике (розмір має не перевищувати 50мб)");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, с этим видео возникли проблемы: видео слишком большое(размер должен не превышать 50мб)");
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
                        text: "Sorry, this video has a problem: the video has an age restriction\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver");
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте, це відео має проблему: відео має вікові обмеження\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, с этим видео возникли проблемы: видео имеет возрастное ограничение\n" +
                        "\nЕсли вы уверенны, что контент публичный или бот ранее уже отправлял это, то напишите нам пожалуйста об этой ошибке - t.me/cobainSaver"
                        );
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

                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                if (audioStreamInfo == null)
                {
                    // Если не найдено подходящих потоков
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this type of audio is not supported, only send me public content",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент",
                            replyParameters: update.Message.MessageId);
                    }
                    return;
                }
                string size = audioStreamInfo.Size.MegaBytes.ToString();
                if (Convert.ToDouble(size) >= 50)
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (update.Message == null)
                    {
                        if (lang == "eng")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Sorry, this audio has a problem: the audio is too big (the size should not exceed 50mb)"
                                );
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, з цим аудіо виникла помилка: аудіо занадто велике (розмір має не перевищувати 50мб)"
                                );
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, с этим аудио возникли проблемы: аудио слишком большое(размер должен не превышать 50мб)"
                                );
                        }
                    }
                    else
                    {
                        if (lang == "eng")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Sorry, this audio has a problem: the audio is too big (the size should not exceed 50mb)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "ukr")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Вибачте, з цим аудіо виникла помилка: аудіо занадто велике (розмір має не перевищувати 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                        if (lang == "rus")
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Извините, с этим аудио возникли проблемы: аудио слишком большое(размер должен не превышать 50мб)",
                                replyParameters: update.Message.MessageId);
                        }
                    }
                    return;
                }
                stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);

                string title = allInfo.Title;
                string thumbnail = "https://img.youtube.com/vi/" + allInfo.Id + "/maxresdefault.jpg";
                string duration = allInfo.Duration.Value.TotalSeconds.ToString();
                string author = allInfo.Author.ToString();
                string toRemove = " - Topic";
                if (author.EndsWith(toRemove))
                {
                    author = author.Substring(0, author.Length - toRemove.Length);
                } 

                string path = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string cleanTitle = MakeValidFileName(title);
                string audioPath = Path.Combine(path, DateTime.Now.Millisecond.ToString() + $"{cleanTitle}" + ".m4a");
                string thumbnailAudioPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);
                try
                {
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
                    if (update.Message == null)
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            title: title,
                            audio: InputFile.FromStream(streamAudio, Path.GetFileName(audioPath)),
                            performer: author,
                            thumbnail: InputFile.FromStream(streamThumbAudio),
                            duration: Convert.ToInt32(duration));
                        if (messageText.StartsWith("spotify "))
                        {
                            await addDB.AddBotCommands(chatId, "spotify", DateTime.Now.ToShortDateString());
                        }
                        else
                        {
                            await addDB.AddBotCommands(chatId, "youtubeMusic", DateTime.Now.ToShortDateString());
                        }
                        
                    }
                    else
                    {
                        await botClient.SendAudioAsync(
                            chatId: chatId,
                            title: title,
                            audio: InputFile.FromStream(streamAudio, Path.GetFileName(audioPath)),
                            performer: author,
                            thumbnail: InputFile.FromStream(streamThumbAudio),
                            duration: Convert.ToInt32(duration),
                            replyParameters: update.Message.MessageId); ;
                        if (messageText.StartsWith("spotify "))
                        {
                            await addDB.AddBotCommands(chatId, "spotify", DateTime.Now.ToShortDateString());
                        }
                        else
                        {
                            await addDB.AddBotCommands(chatId, "youtubeMusic", DateTime.Now.ToShortDateString());
                        }
                    }
                }
                catch (Exception ex)
                { //await Console.Out.WriteLineAsync(ex.ToString());
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this type of audio is not supported, only send me public content",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент",
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
                if (update.Message == null)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this type of audio is not supported, only send me public content"
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент"
                            );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент"
                            );
                    }
                }
                else
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this type of audio is not supported, only send me public content",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип аудіо не підтримується, надсилайте мені тільки публічний контент",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот тип аудио не поддерживается, отправляйте мне только публичный контент",
                            replyParameters: update.Message.MessageId);
                    }
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
        public async Task YoutubeMusicPlaylist(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, int page, int msgId)
        {
            try
            {
                AddToDataBase addDB = new AddToDataBase();
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());

                Message message = null;

                string normallMsg = await DeleteNotUrl(messageText);
                var youtube = new YoutubeClient();
                var allInfo = await youtube.Playlists.GetAsync(normallMsg);
                var videoUrl = normallMsg;
                var videosSubset = await youtube.Playlists.GetVideosAsync(videoUrl);
                string thumbnail = "https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg";
                string songsCount = null;
                if (videosSubset.Count % 10 == 1 && videosSubset.Count % 100 != 11)
                {
                    if (lang == "eng")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " song";
                    }
                    if (lang == "ukr")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " трек";
                    }
                    if (lang == "rus")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " трек";
                    }
                }
                else if ((videosSubset.Count % 10 == 2 || videosSubset.Count % 10 == 3 || videosSubset.Count % 10 == 4) && (videosSubset.Count % 100 < 10 || videosSubset.Count % 100 >= 20))
                {
                    if (lang == "eng")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " трека";
                    }
                    if (lang == "rus")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " трека";
                    }
                }
                else
                {
                    if (lang == "eng")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " треків";
                    }
                    if (lang == "rus")
                    {
                        songsCount = $"*{videosSubset.Count}*" + " треков";
                    }
                }
                foreach (var thumb in allInfo.Thumbnails)
                {
                    thumbnail = thumb.Url;
                }
                // Определяем текущую страницу и размер страницы
                int pageSize = 10;
                int currentPage = 0;
                if (page == 0)
                {
                    currentPage = page;
                    currentPage++;
                }
                else
                {
                    currentPage = page;
                }
                int totalPages = (int)Math.Ceiling((double)videosSubset.Count / pageSize);

                if (page < 0)
                {
                    currentPage = totalPages;
                }
                if(page > totalPages)
                {
                    currentPage = 1;
                }

                // Формируем список песен для текущей страницы
                var songsForCurrentPage = videosSubset.Skip((currentPage - 1) * pageSize).Take(pageSize);

                // Создаем кнопки для текущей страницы
                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard;

                foreach (var music in songsForCurrentPage)
                {
                    buttonsList.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: music.Title, callbackData: "L" + " " + music.Id + " " + chatId),
                    });
                }


                string path = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string thumbnailPath = Path.Combine(path, chatId + DateTime.Now.Millisecond.ToString() + "thumbVideo.jpeg");
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(thumbnail, thumbnailPath);
                    }
                }
                catch (Exception e)
                {
                }
                if (!System.IO.File.Exists(thumbnailPath))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile("https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg", thumbnailPath);
                    }
                }
                await using Stream streamThumb = System.IO.File.OpenRead(thumbnailPath);


                if (page == 0)
                {
                    message = await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: InputFile.FromStream(streamThumb),
                        caption: $"Choose song in {allInfo.Title}, Page {currentPage} of {totalPages}"
                    );
                    streamThumb.Close();
                    System.IO.File.Delete(thumbnailPath);
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if(totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                    }

                    inlineKeyboard = new InlineKeyboardMarkup(buttonsList);


                    // Отправляем сообщение с кнопками
                    if (lang == "eng")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: message.MessageId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Page *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Select a song to download"
                        );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: message.MessageId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Сторінка *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Виберіть пісню"
                        );
                    }
                    if (lang == "rus")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: message.MessageId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }
                }
                else if(page != 0)
                {
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if(totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "P" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "N" + " " + chatId + " " + allInfo.Id + " " + currentPage + " " + msgId)
                        });
                        }
                    }

                    inlineKeyboard = new InlineKeyboardMarkup(buttonsList);

                    // Отправляем сообщение с кнопками
                    if(lang == "eng")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: msgId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Page *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Select a song to download"
                        );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: msgId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Сторінка *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Виберіть пісню"
                        );
                    }
                    if (lang == "rus")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: msgId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(allInfo.Title)}*\n" +
                            $"\n" +
                            $"{songsCount}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }

                }

            }
            catch (Exception e)
            {
                //await Console.Out.WriteLineAsync(e.ToString());
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (update.Message == null)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this playlist is unavailable, only send me public content"
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип плейлист недоступний, надсилайте мені тільки публічний контент"
                            );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот плейлист недоступен, отправляйте мне только публичный контент"
                            );
                    }
                }
                else
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Sorry, this playlist is unavailable, only send me public content",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вибачте, цей тип плейлист недоступний, надсилайте мені тільки публічний контент",
                            replyParameters: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, этот плейлист недоступен, отправляйте мне только публичный контент",
                            replyParameters: update.Message.MessageId);
                    }
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
        public async Task<string> EscapeMarkdownV2(string input)
        {
            // Dictionary of characters needing escaping and their replacements
            Dictionary<string, string> escapeCharacters = new Dictionary<string, string>
            {
                { "\\", "\\\\" },
                { "_", "\\_" },
                { "*", "\\*" },
                { "[", "\\[" },
                { "]", "\\]" },
                { "(", "\\(" },
                { ")", "\\)" },
                { "~", "\\~" },
                { "`", "\\`" },
                { ">", "\\>" },
                { "#", "\\#" },
                { "+", "\\+" },
                { "-", "\\-" },
                { "=", "\\=" },
                { "|", "\\|" },
                { "{", "\\{" },
                { "}", "\\}" },
                { ".", "\\." },
                { "!", "\\!" },
            };

            // Iterate through the dictionary and perform replacements using regular expressions
            foreach (var pair in escapeCharacters)
            {
                input = input.Replace(pair.Key, pair.Value);
            }

            return input;
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
