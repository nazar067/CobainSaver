using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using YoutubeSearchApi.Net.Models.Youtube;
using YoutubeSearchApi.Net.Services;
using System.Text.RegularExpressions;
using CobainSaver.DataBase;
using Telegram.Bot.Types.ReplyMarkups;
using System.Dynamic;
using Telegram.Bot.Types.Enums;
using System.Net;
using YoutubeExplode.Common;
using AngleSharp.Common;

namespace CobainSaver.Downloader
{
    internal class Spotify
    {
        public async Task SpotifyGetName(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                HttpClient spotifyClient = new HttpClient();

                string client_id = jsonObjectAPI["Spotify"][0].ToString();
                string client_secret = jsonObjectAPI["Spotify"][1].ToString();

                string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));

                string url = await DeleteNotUrl(messageText);

                string id = await ExtractTrackId(url);

                var authOptions = new
                {
                    url = "https://accounts.spotify.com/api/token",
                    headers = new
                    {
                        Authorization = "Basic " + base64Auth
                    },
                    form = new
                    {
                        grant_type = "client_credentials"
                    }
                };

                string accessToken = await GetAccessToken(authOptions, base64Auth);
                spotifyClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var spotify = new SpotifyClient(accessToken);

                var track = await spotify.Tracks.Get(id);
                string artist = null;
                foreach (var artists in track.Artists)
                {
                    artist = artists.Name;
                }
                await FindSongYTMusic(artist + " " + track.Name + " audio", chatId, update, cancellationToken, botClient);
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
                        text: "Sorry this song was not found or private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте ця пісня не знайдена або приватна\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините эта песня не найдена или приватная\n" +
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

        public async Task SpotifyGetAlbum(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, int page, int msgId)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                AddToDataBase addDB = new AddToDataBase();

                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());

                Message message = null;

                HttpClient spotifyClient = new HttpClient();

                string client_id = jsonObjectAPI["Spotify"][0].ToString();
                string client_secret = jsonObjectAPI["Spotify"][1].ToString();

                string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));

                string url = await DeleteNotUrl(messageText);

                if (url.Contains("playlist"))
                {
                    await SpotifyGetPlaylist(chatId, update, cancellationToken, messageText, botClient, page, msgId);
                    return;
                }

                string id = await ExtractTrackId(url);

                var authOptions = new
                {
                    url = "https://accounts.spotify.com/api/token",
                    headers = new
                    {
                        Authorization = "Basic " + base64Auth
                    },
                    form = new
                    {
                        grant_type = "client_credentials"
                    }
                };

                string accessToken = await GetAccessToken(authOptions, base64Auth);
                spotifyClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var spotify = new SpotifyClient(accessToken);

                var album = await spotify.Albums.Get(id);

                string albumName = album.Name;
                string albumArtist = null;
                string albumThumb = null;
                int countTracks = album.TotalTracks;
                string countTrackText = null;

                foreach ( var thumb in album.Images)
                {
                    albumThumb = thumb.Url;
                    break;
                }
                foreach ( var artist in album.Artists)
                {
                    albumArtist += artist.Name + " ";
                }
                if (countTracks % 10 == 1 && countTracks % 100 != 11)
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " song";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " трек";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " трек";
                    }
                }
                else if ((countTracks % 10 == 2 || countTracks % 10 == 3 || countTracks % 10 == 4) && (countTracks % 100 < 10 || countTracks % 100 >= 20))
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " трека";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " трека";
                    }
                }
                else
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " треків";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " треков";
                    }
                }
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
                int totalPages = (int)Math.Ceiling((double)countTracks / pageSize);

                if (page < 0)
                {
                    currentPage = totalPages;
                }
                if (page > totalPages)
                {
                    currentPage = 1;
                }

                var songsForCurrentPage = album.Tracks.Items.Skip((currentPage - 1) * pageSize).Take(pageSize);

                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard;

                string songArtist = null;

                foreach (var song in songsForCurrentPage)
                {
                    foreach (var currentSongArtist in song.Artists)
                    {
                        songArtist = currentSongArtist.Name;
                        break;
                    }
                    string directory = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "audio");
                    string uniqueFileName = GenerateUniqueFileName(directory, ".txt");
                    Logs logs = new Logs(chatId, 0, "", songArtist + " " + song.Name, "");
                    await logs.WriteSpotifySongInfo(songArtist + " " + song.Name, uniqueFileName);
                    buttonsList.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: song.Name, callbackData: "S" + " " + chatId + " " + uniqueFileName),
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
                        client.DownloadFile(albumThumb, thumbnailPath);
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
                        caption: $"Choose song in {albumName}, Page {currentPage} of {totalPages}"
                    );
                    streamThumb.Close();
                    System.IO.File.Delete(thumbnailPath);
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if (totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + message.MessageId)
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
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }
                }
                else if (page != 0)
                {
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if (totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNA" + " " + chatId + " " + album.Id + " " + currentPage + " " + msgId)
                        });
                        }
                    }

                    inlineKeyboard = new InlineKeyboardMarkup(buttonsList);

                    // Отправляем сообщение с кнопками
                    if (lang == "eng")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: msgId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(albumName)}*\n" +
                            $"\n" +
                            $"*{albumArtist}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }

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
                        text: "Sorry this song was not found or private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте ця пісня не знайдена або приватна\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините эта песня не найдена или приватная\n" +
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

        public async Task SpotifyGetPlaylist(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, int page, int msgId)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                AddToDataBase addDB = new AddToDataBase();

                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());

                Message message = null;

                HttpClient spotifyClient = new HttpClient();

                string client_id = jsonObjectAPI["Spotify"][0].ToString();
                string client_secret = jsonObjectAPI["Spotify"][1].ToString();

                string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));

                string url = await DeleteNotUrl(messageText);


                string id = await ExtractTrackId(url);

                var authOptions = new
                {
                    url = "https://accounts.spotify.com/api/token",
                    headers = new
                    {
                        Authorization = "Basic " + base64Auth
                    },
                    form = new
                    {
                        grant_type = "client_credentials"
                    }
                };

                string accessToken = await GetAccessToken(authOptions, base64Auth);
                spotifyClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var spotify = new SpotifyClient(accessToken);

                var playlist = await spotify.Playlists.Get(id);

                string playlistName = playlist.Name;
                string playlistOwner = playlist.Owner.DisplayName;
                string playlistThumb = null;
                int countTracks = 0;
                string countTrackText = null;

                foreach (PlaylistTrack<IPlayableItem> item in playlist.Tracks.Items)
                {
                    if (item.Track is FullTrack track)
                    {
                        countTracks++;
                    }
                }

                foreach (var thumb in playlist.Images)
                {
                    playlistThumb = thumb.Url;
                    break;
                }
                if (countTracks % 10 == 1 && countTracks % 100 != 11)
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " song";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " трек";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " трек";
                    }
                }
                else if ((countTracks % 10 == 2 || countTracks % 10 == 3 || countTracks % 10 == 4) && (countTracks % 100 < 10 || countTracks % 100 >= 20))
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " трека";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " трека";
                    }
                }
                else
                {
                    if (lang == "eng")
                    {
                        countTrackText = $"*{countTracks}*" + " songs";
                    }
                    if (lang == "ukr")
                    {
                        countTrackText = $"*{countTracks}*" + " треків";
                    }
                    if (lang == "rus")
                    {
                        countTrackText = $"*{countTracks}*" + " треков";
                    }
                }
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
                int totalPages = (int)Math.Ceiling((double)countTracks / pageSize);

                if (page < 0)
                {
                    currentPage = totalPages;
                }
                if (page > totalPages)
                {
                    currentPage = 1;
                }

                var songsForCurrentPage = playlist.Tracks.Items.Skip((currentPage - 1) * pageSize).Take(pageSize);

                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard;

                string songArtist = null;

                foreach (PlaylistTrack<IPlayableItem> item in songsForCurrentPage)
                {
                    if (item.Track is FullTrack track)
                    {
                        foreach (var currentSongArtist in track.Artists)
                        {
                            songArtist = currentSongArtist.Name;
                            break;
                        }
                        string directory = Path.Combine(Directory.GetCurrentDirectory(), "UserLogs", chatId.ToString(), "spotify");
                        string uniqueFileName = GenerateUniqueFileName(directory, ".txt");
                        Logs logs = new Logs(chatId, 0, "", songArtist + " " + track.Name, "");
                        await logs.WriteSpotifySongInfo(songArtist + " " + track.Name, uniqueFileName);
                        buttonsList.Add(new[]
                        {
                        InlineKeyboardButton.WithCallbackData(text: track.Name, callbackData: "S" + " " + chatId + " " + uniqueFileName),
                    });
                    }
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
                        client.DownloadFile(playlistThumb, thumbnailPath);
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
                        caption: $"Choose song in {playlist.Name}, Page {currentPage} of {totalPages}"
                    );
                    streamThumb.Close();
                    System.IO.File.Delete(thumbnailPath);
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if (totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + message.MessageId)
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
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }
                }
                else if (page != 0)
                {
                    // Добавляем кнопки "назад" и "вперед", если необходимо
                    if (totalPages > 1)
                    {
                        if (lang == "eng")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Back", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Next ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "ukr")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId)
                        });
                        }
                        if (lang == "rus")
                        {
                            buttonsList.Add(new[]
                            {
                            InlineKeyboardButton.WithCallbackData(text: "◀️ Назад", callbackData: "SPP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId),
                            InlineKeyboardButton.WithCallbackData(text: "Вперед ▶️", callbackData: "SNP" + " " + chatId + " " + playlist.Id + " " + currentPage + " " + msgId)
                        });
                        }
                    }

                    inlineKeyboard = new InlineKeyboardMarkup(buttonsList);

                    // Отправляем сообщение с кнопками
                    if (lang == "eng")
                    {
                        await botClient.EditMessageCaptionAsync(
                            messageId: msgId,
                            chatId: chatId,
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.MarkdownV2,
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
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
                            caption: $"*{await EscapeMarkdownV2(playlistName)}*\n" +
                            $"\n" +
                            $"*{playlistOwner}*" +
                            $"\n" +
                            $"{countTrackText}\n" +
                            $"_Страница *{currentPage}/{totalPages}*_" +
                            $"\n" +
                            $"\n⬇️ Выберите песню"
                        );
                    }

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
                        text: "Sorry this song was not found or private\n" +
                        "\nIf you're sure the content is public or the bot has previously submitted this, please email us about this bug - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вибачте ця пісня не знайдена або приватна\n" +
                        "\nЯкщо ви впевнені, що контент публічний або бот раніше вже відправляв це, то напишіть нам, будь ласка, про цю помилку - t.me/cobainSaver",
                        replyToMessageId: update.Message.MessageId);
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините эта песня не найдена или приватная\n" +
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

        public async Task FindSongYTMusic(string info, long chatId, Update update, CancellationToken cancellationToken, TelegramBotClient botClient)
        {
            YouTube youTube = new YouTube();
            using (var httpClient = new HttpClient())
            {
                YoutubeSearchClient client = new YoutubeSearchClient(httpClient);

                var responseObject = await client.SearchAsync(info);

                string videoId = null;

                foreach (YoutubeVideo video in responseObject.Results)
                {
                    videoId = video.Id;
                    break;
                }
                string song = "spotify " + "https://music.youtube.com/watch?v=" + videoId;

                await youTube.YoutubeMusicDownloader(chatId, update, cancellationToken, song, botClient);
            }
        }
        static async Task<string> GetAccessToken(dynamic authOptions, string base64Auth)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Auth);

                var response = await client.PostAsync(authOptions.url, content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                    return json.access_token;
                }
                else
                {
                    Console.WriteLine("Ошибка при запросе токена: " + response.StatusCode);
                    return null;
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
        public async Task<string> ExtractTrackId(string url)
        {
            // Используем регулярное выражение для извлечения части строки между "track/" и "?"
            string pattern = @"(?:track|album|playlist)/([^?]+)";
            Match match = Regex.Match(url, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty; // Возвращаем пустую строку, если совпадений не найдено
        }
        private static string GenerateUniqueFileName(string directoryPath, string extension, int length = 15)
        {
            string fileName;
            do
            {
                fileName = GenerateRandomString(length) + extension;
            }
            while (System.IO.File.Exists(Path.Combine(directoryPath, fileName)));

            return fileName;
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var stringBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[random.Next(chars.Length)]);
            }
            return stringBuilder.ToString();
        }
    }
}
