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
                var cobain = await botClient.GetMeAsync();
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
                var cobain = await botClient.GetMeAsync();
                // эта переменная будет содержать в себе все связанное с сообщениями
                var message = update.Message;
                Downloader video = new Downloader();
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
                    await video.YoutubeDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://music.youtube.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVoice);
                    await video.YoutubeMusicDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://open.spotify.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVoice);
                    await video.SpotifyDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://vm.tiktok.com") || message.Text.Contains("https://www.tiktok.com") || message.Text.Contains("https://m.tiktok.com"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await video.TikTokDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://www.reddit.com") || message.Text.Contains("https://redd.it/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await video.ReditDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://x.com/") || message.Text.Contains("https://twitter.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await video.TwitterDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://www.instagram.com"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadDocument);
                    await video.InstagramDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.Contains("https://rt.pornhub.com/"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.UploadVideo);
                    await video.PornHubDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                }
                else if (message.Text.StartsWith("/logs") || message.Text.StartsWith($"/logs@{cobain.Username}"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    await logs.SendAllYears((TelegramBotClient)botClient, chat.Id.ToString(), 0, chat.Id.ToString());
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
                            text: "/help - see all commands\n /logs - look at chat server logs\n " +
                            "/changelang - change bot's language",
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "/help - переглянути всі команді\n /logs - переглянути ваші логи\n " +
                            "/changelang - змінити мову",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chat.Id,
                            text: "/help - посмотреть все команді\n /logs - посмотреть ваши логи\n " +
                            "/changelang - сменить язык",
                            replyToMessageId: update.Message.MessageId);
                    }
                }
                else if (message.Text == "/countUsers")
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await logs.CountAllUsers(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                }
                else if (message.Text.StartsWith("/userLogs"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    if (!dateLog.Contains("/") && !dateLog.Contains("."))
                        dateLog = "/userLogs ";
                    await logs.SendUserLogsToAdmin(message.Text, dateLog, chat.Id.ToString(), update, cancellationToken, message, (TelegramBotClient)botClient, cobain.Username);
                }
                else if (message.Text == "/serverLogs")
                {
                    await logs.SendServerLogs(chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                }
                else if (message.Text == "/lastTen")
                {
                    await logs.SendLastTenUsers((TelegramBotClient)botClient, chat.Id.ToString());
                }
                else if (message.Text.StartsWith("/uniqUsers"))
                {
                    string dateLog = message.Text.Split(' ').Last();
                    await logs.CountUniqUsers(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username); ;
                }
                else if (message.Text == "/changelang" || message.Text.StartsWith($"/changelang@{cobain.Username}"))
                {
                    await botClient.SendChatActionAsync(chat.Id, ChatAction.Typing);
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                                    // first row
                                    new []
                                    {
                                        InlineKeyboardButton.WithCallbackData(text: "Українська", callbackData: "ukr"),
                                        InlineKeyboardButton.WithCallbackData(text: "English", callbackData: "eng"),
                                        InlineKeyboardButton.WithCallbackData(text: "Русский", callbackData: "rus"),
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
                }
                Reviews reviews = new Reviews();
                await reviews.UserReviews(chat.Id.ToString(), (TelegramBotClient)botClient);
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
        private static async Task CheckCallbackQuery(Update update, TelegramBotClient botClient, CancellationToken cancellationToken)
        {
            try
            {
                var callbackQuery = update.CallbackQuery;
                if (callbackQuery.Data.Contains("ukr"))
                {
                    string msg = "Мова змінена";
                    Language language = new Language(callbackQuery.Data, msg);
                    await language.ChangeLanguage(callbackQuery.Message.Chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.Contains("eng"))
                {
                    string msg = "Language has been changed";
                    Language language = new Language(callbackQuery.Data, msg);
                    await language.ChangeLanguage(callbackQuery.Message.Chat.Id.ToString(), (TelegramBotClient)botClient);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                if (callbackQuery.Data.Contains("rus"))
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
                    await botClient.SendChatActionAsync(chatId, ChatAction.UploadDocument);
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
                    await reviews.LogsUserReviews(pollAnswer.PollId, 1, 0, 0, 0, 0, userId);
                }
                else if (pollAnswer.OptionIds.Contains(1))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, 0, 1, 0, 0, 0, userId);
                }
                else if (pollAnswer.OptionIds.Contains(2))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, 0, 0, 1, 0, 0, userId);
                }
                else if (pollAnswer.OptionIds.Contains(3))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, 0, 0, 0, 1, 0, userId);
                }
                else if (pollAnswer.OptionIds.Contains(4))
                {
                    await reviews.LogsUserReviews(pollAnswer.PollId, 0, 0, 0, 0, 1, userId);
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