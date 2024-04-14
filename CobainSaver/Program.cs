using CobainSaver.DataBase;
using CobainSaver.Downloader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;

namespace CobainSaver
{
    class Program
    {
        //private static ITelegramBotClient _botClient;

        // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
        private static ReceiverOptions _receiverOptions;

        static async Task Main()
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            var botClient = new TelegramBotClient(jsonObject["BotAPI"][0].ToString());
            _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
            {
                AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
                {
                UpdateType.Message,// Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
                UpdateType.CallbackQuery,
                UpdateType.PollAnswer,
                UpdateType.Poll
            },
                // Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
                // True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();

            // UpdateHander - обработчик приходящих Update`ов
            // ErrorHandler - обработчик ошибок, связанных с Bot API
            botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // Запускаем бота

            var cobain = await botClient.GetMeAsync(); // Создаем переменную, в которую помещаем информацию о нашем боте.
            Console.WriteLine($"Cobain here");

            await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно
        }
        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try
            {
                //var cobain = await botClient.GetMeAsync();
                // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            Task.Run(async () => await CheckMsg(update, (TelegramBotClient)botClient, cancellationToken));
                            break;
                        }
                    case UpdateType.CallbackQuery:
                        {
                            Task.Run(async () => await CheckCallbackQuery(update, (TelegramBotClient)botClient, cancellationToken));
                            break;
                        }
                    case UpdateType.PollAnswer:
                        {
                            Task.Run(async () => await CheckPollAnswer(update, (TelegramBotClient)botClient, cancellationToken));
                            break;
                        }
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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        private static async Task CheckMsg(Update update, TelegramBotClient botClient, CancellationToken cancellationToken)
        {
            try
            {
                User? cobain = null;
                try
                {
                    cobain = await botClient.GetMeAsync();
                }
                catch(Exception ex) 
                {
                    try
                    {
                        var msg = update.Message;
                        var usr = msg.From;
                        var cht = msg.Chat;
                        Logs log = new Logs(cht.Id, usr.Id, usr.Username, null, ex.ToString());
                        await log.WriteServerLogs();
                    }
                    catch (Exception e)
                    {
                    }
                }
                // эта переменная будет содержать в себе все связанное с сообщениями
                var message = update.Message;
                Instagram insta = new Instagram();
                PornHub porn = new PornHub();
                Reddit reddit = new Reddit();
                Spotify spotify = new Spotify();
                TikTok tikTok = new TikTok();
                Twitter twitter = new Twitter();
                YouTube youTube = new YouTube();
                Pinterest pinterest = new Pinterest();
                AddToDataBase addDB = new AddToDataBase();
                AdminCommands admin = new AdminCommands();
                //Downloader video = new Downloader();
                // From - это от кого пришло сообщение (или любой другой Update)
                var user = message.From;
                var chat = message.Chat;
                Logs logs = new Logs(message.Chat.Id, message.From.Id, message.From.Username, message.Text, null);
                await logs.WriteUserLogs();
                await logs.WriteLastUsers();

                if (message.Text.Contains("https://www.youtube.com") || message.Text.Contains("https://youtu.be") 
                    || message.Text.Contains("https://youtube.com") || message.Text.Contains("https://m.youtube.com") 
                    || message.Text.Contains("youtu.be/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVideo);
                    await youTube.YoutubeDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "youtube", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://music.youtube.com/playlist?"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    await youTube.YoutubeMusicPlaylist(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient, 0, 0);
                    await addDB.AddUserLinks(chat.Id, user.Id, "youtubeMusic", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://music.youtube.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVoice);
                    await youTube.YoutubeMusicDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "youtubeMusic", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://open.spotify.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVoice);
                    await spotify.SpotifyDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "spotify", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://vm.tiktok.com") || message.Text.Contains("https://www.tiktok.com") || message.Text.Contains("https://m.tiktok.com"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await tikTok.TikTokDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "tiktok", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://www.reddit.com") || message.Text.Contains("https://redd.it/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await reddit.ReditDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "reddit", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://x.com/") || message.Text.Contains("https://twitter.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await twitter.TwitterDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "twitter", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://www.instagram.com"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await insta.InstagramDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "instagram", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://rt.pornhub.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVideo);
                    await porn.PornHubDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "pornhub", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.Contains("https://www.pinterest.com") || message.Text.Contains("https://pin.it/") || message.Text.Contains("https://ru.pinterest.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await pinterest.PinterestDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                    await addDB.AddUserLinks(chat.Id, user.Id, "pinterest", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/logs") || message.Text.StartsWith($"/logs@{cobain.Username}"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    await logs.SendAllYears((TelegramBotClient)botClient, chat.Id.ToString(), 0, chat.Id.ToString());
                    await addDB.AddUserCommands(chat.Id, user.Id, "logs", message.MessageId, DateTime.Now.ToShortDateString());
                    //string dateLog = message.Text.Split(' ').Last();
                    // await logs.SendUserLogs(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                }
                else if (message.Text == "/start" || message.Text.StartsWith($"/start@{cobain.Username}"))
                {
                    Language language = new Language(user.LanguageCode, null);
                    await language.StartLanguage(chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    string lang = await language.GetCurrentLanguage(chat.Id.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "Hi, I'm CobainSaver, just send me video's link",
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "Привіт, я CobainSaver, відправ мені посилання на відео",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "Привет, я CobainSaver, отправь мне ссылку на видео",
                            replyToMessageId: update.Message.MessageId);
                    }
                    await addDB.AddUserCommands(chat.Id, user.Id, "start", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/help" || message.Text.StartsWith($"/help@{cobain.Username}"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chat.Id.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "/help - see all commands\n " +
                            "/changelang - change bot's language",
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "/help - переглянути всі команді\n " +
                            "/changelang - змінити мову",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "/help - посмотреть все команды\n " +
                            "/changelang - сменить язык",
                            replyToMessageId: update.Message.MessageId);
                    }
                    await addDB.AddUserCommands(chat.Id, user.Id, "help", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/countUsers")
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.CountAllUsers(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                    await addDB.AddUserCommands(chat.Id, user.Id, "countUsers", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/userLogs"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    if (!dateLog.Contains("/") && !dateLog.Contains("."))
                        dateLog = "/userLogs ";
                    await logs.SendUserLogsToAdmin(message.Text, dateLog, chat.Id.ToString(), update, cancellationToken, message, (TelegramBotClient)botClient, cobain.Username);
                    await addDB.AddUserCommands(chat.Id, user.Id, "userLogs", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/serverLogs")
                {
                    await admin.SendServerLogs(chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                    await addDB.AddUserCommands(chat.Id, user.Id, "serverLogs", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/lastTen")
                {
                    await admin.SendLastTenUsers((TelegramBotClient)botClient, chat.Id.ToString());
                    await addDB.AddUserCommands(chat.Id, user.Id, "lastTen", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/uniqUsers"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.CountUniqUsers(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username); ;
                    await addDB.AddUserCommands(chat.Id, user.Id, "uniqUsers", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/uniqChats"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.CountUniqChats(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username); ;
                    await addDB.AddUserCommands(chat.Id, user.Id, "uniqChats", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/sendAll"))
                {
                    await admin.SendMsgToAllUsers(chat.Id.ToString(), (TelegramBotClient)botClient, update);
                    await addDB.AddUserCommands(chat.Id, user.Id, "sendAll", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/reviews"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.SendAllRewies(chat.Id.ToString(), update, cancellationToken, dateLog, (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "sendAllReviews", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/changelangAll")
                {
                    Language language = new Language("rand", "rand");
                    await language.ChangeLanguageAllUsers(chat.Id.ToString(), (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "changelangAll", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/service")
                {
                    await admin.ServiceStatistic(chat.Id.ToString(), update, cancellationToken, (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "service", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/serviceBot")
                {
                    await admin.ServiceBotStatistic(chat.Id.ToString(), update, cancellationToken, (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "serviceBot", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/countLinks"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.CountAllLinks(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                    await addDB.AddUserCommands(chat.Id, user.Id, "countLinks", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/countBotLinks"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await admin.CountBotLinks(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                    await addDB.AddUserCommands(chat.Id, user.Id, "countBotLinks", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/lang")
                {
                    await admin.LanguageStatistics(chat.Id.ToString(), update, cancellationToken, (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "lang", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/topTen")
                {
                    await admin.ChatStatistic(chat.Id.ToString(), update, cancellationToken, (TelegramBotClient)botClient);
                    await addDB.AddUserCommands(chat.Id, user.Id, "topTen", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text.StartsWith("/user"))
                {
                    string userId = message.Text.Split(' ').Last();
                    await admin.CheckUserById((TelegramBotClient)botClient, chat.Id.ToString(), userId);
                    await addDB.AddUserCommands(chat.Id, user.Id, "topTen", message.MessageId, DateTime.Now.ToShortDateString());
                }
                else if (message.Text == "/changelang" || message.Text.StartsWith($"/changelang@{cobain.Username}"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                                    // first row
                                    new []
                                    {
                                        InlineKeyboardButton.WithCallbackData(text: "Українська", callbackData: "uk"),
                                        InlineKeyboardButton.WithCallbackData(text: "English", callbackData: "en"),
                                        InlineKeyboardButton.WithCallbackData(text: "Русский", callbackData: "ru"),
                                    },
                                });
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chat.Id.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            replyMarkup: inlineKeyboard,
                            text: "Choose language"
                        );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            replyMarkup: inlineKeyboard,
                            text: "Виберіть мову"
                        );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            replyMarkup: inlineKeyboard,
                            text: "Выберите язык"
                        );
                    }
                    await addDB.AddUserCommands(chat.Id, user.Id, "changelang", message.MessageId, DateTime.Now.ToShortDateString());
                }
                Reviews reviews = new Reviews();
                await reviews.UserReviews(chat.Id.ToString(), (TelegramBotClient)botClient);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                try
                {
                    if(!ex.ToString().Contains("System.NullReferenceException: Object reference not set to an instance of an object.\r\n   at CobainSaver.Program.CheckMsg(Update update, TelegramBotClient botClient, CancellationToken cancellationToken) in C:\\Users\\gamer\\source\\repos\\CobainSaver\\CobainSaver\\Program.cs"))
                    {
                        var message = update.Message;
                        var user = message.From;
                        var chat = message.Chat;
                        Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                        await logs.WriteServerLogs();
                    }
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        private static async Task CheckCallbackQuery(Update update, TelegramBotClient botClient, CancellationToken cancellationToken)
        {
            try
            {
                var callbackQuery = update.CallbackQuery;
                if (callbackQuery.Data.Contains("uk"))
                {
                    string msg = "Мова змінена";
                    Language language = new Language(callbackQuery.Data, msg);
                    await language.ChangeLanguage(callbackQuery.Message.Chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.Contains("en"))
                {
                    string msg = "Language has been changed";
                    Language language = new Language(callbackQuery.Data, msg);
                    await language.ChangeLanguage(callbackQuery.Message.Chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.Contains("ru"))
                {
                    string msg = "Язык изменен";
                    Language language = new Language(callbackQuery.Data, msg);
                    await language.ChangeLanguage(callbackQuery.Message.Chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("Year"))
                {
                    Logs logs = new Logs(1, 1, null, null, null);
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string year = parts[1];
                    //string date = fileName.Replace(".txt", "");
                    string chatId = parts[2];
                    string messageId = parts[3];
                    string chatToSend = parts[4];
                    await logs.SendAllMonths((TelegramBotClient)botClient, chatId, year, Convert.ToInt32(messageId), chatToSend);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("Month"))
                {
                    Logs logs = new Logs(1, 1, null, null, null);
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string month = parts[1];
                    string year = parts[2];
                    string chatId = parts[3];
                    string messageId = parts[4];
                    string chatToSend = parts[5];
                    await logs.SendAllDates((TelegramBotClient)botClient, chatId, year, month, Convert.ToInt32(messageId), chatToSend);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("Date"))
                {
                    Logs logs = new Logs(1, 1, null, null, null);
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string chatId = parts[1];
                    string month = parts[2];
                    string fileName = parts[3];
                    string date = fileName.Replace(".txt", "");
                    string year = parts[4];
                    string chatToSend = parts[5];
                    await logs.SendUserLogs(year, month, date, chatId, update, (TelegramBotClient)botClient, chatToSend);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("BackToYear"))
                {
                    Logs logs = new Logs(1, 1, null, null, null);
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string chatId = parts[1];
                    string messageId = parts[2];
                    string chatToSend = parts[3];
                    await logs.SendAllYears((TelegramBotClient)botClient, chatId.ToString(), Convert.ToInt32(messageId), chatToSend);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("BackToMonth"))
                {
                    Logs logs = new Logs(1, 1, null, null, null);
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string chatId = parts[1];
                    string year = parts[2];
                    string messageId = parts[3];
                    string chatToSend = parts[4];
                    await logs.SendAllMonths((TelegramBotClient)botClient, chatId.ToString(), year, Convert.ToInt32(messageId), chatToSend);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("L "))
                {
                    AddToDataBase addDB = new AddToDataBase();
                    YouTube youTube = new YouTube();
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string url = parts[1];
                    string chatId = parts[2];
                    url = "https://music.youtube.com/watch?v=" + url;
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadVoice);
                    await youTube.YoutubeMusicDownloader(Convert.ToInt64(chatId), update, cancellationToken, url, (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    await addDB.AddUserLinks(Convert.ToInt64(chatId), Convert.ToInt64(chatId), "youtubeMusic", 0, DateTime.Now.ToShortDateString());
                }
                if (callbackQuery.Data.StartsWith("N"))
                {
                    YouTube youTube = new YouTube();
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string chatId = parts[1];
                    string url = parts[2];
                    url = "https://music.youtube.com/playlist?list=" + url;
                    int page = Convert.ToInt32(parts[3]);
                    page++;
                    int msgId = Convert.ToInt32(parts[4]);
                    await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                    await youTube.YoutubeMusicPlaylist(Convert.ToInt64(chatId), update, cancellationToken, url, (TelegramBotClient)botClient, page, msgId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.StartsWith("P"))
                {
                    YouTube youTube = new YouTube();
                    string data = callbackQuery.Data.ToString();
                    string[] parts = data.Split(' ');
                    string chatId = parts[1];
                    string url = parts[2];
                    url = "https://music.youtube.com/playlist?list=" + url;
                    int page = Convert.ToInt32(parts[3]);
                    page--;
                    int msgId = Convert.ToInt32(parts[4]);
                    await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                    await youTube.YoutubeMusicPlaylist(Convert.ToInt64(chatId), update, cancellationToken, url, (TelegramBotClient)botClient, page, msgId);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        private static async Task CheckPollAnswer(Update update, TelegramBotClient botClient, CancellationToken cancellationToken)
        {
            try
            {
                var pollAnswer = update.PollAnswer;
                string userId = pollAnswer.User.Id.ToString();
                Reviews reviews = new Reviews();
                if (pollAnswer.OptionIds.Contains(0))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, pollAnswer.OptionIds[0], userId);
                }
                else if (pollAnswer.OptionIds.Contains(1))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, pollAnswer.OptionIds[0], userId);
                }
                else if (pollAnswer.OptionIds.Contains(2))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, pollAnswer.OptionIds[0], userId);
                }
                else if (pollAnswer.OptionIds.Contains(3))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, pollAnswer.OptionIds[0], userId);
                }
                else if (pollAnswer.OptionIds.Contains(4))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, pollAnswer.OptionIds[0], userId);
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
                    Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}