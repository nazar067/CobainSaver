using CobainSaver.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using VideoLibrary;
using Telegram.Bot.Types;
using System.Net;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http;

namespace CobainSaver
{
    internal class AdminCommands
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);

        static string torProxyUrl = jsonObjectAPI["Proxy"][3].ToString();

        static WebProxy torProxy = new WebProxy
        {
            Address = new Uri(torProxyUrl),
        };
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = torProxy,
            UseCookies = false
        };
        private static readonly HttpClient client = new HttpClient(handler);
        public async Task CountAllUsers(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

                string[] directories = Directory.GetDirectories(currentDirectory);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: directories.Length.ToString(),
                    replyToMessageId: update.Message.MessageId
                    );
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Подсчитываем количество уникальных значений в столбце user_id
                    int uniqueUserCount = db.UserLinks.Select(item => item.user_id).Distinct().Count();

                    // Выводим результат
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Data base: " + uniqueUserCount.ToString(),
                        replyToMessageId: update.Message.MessageId
                    );
                }
            }
        }
        public async Task CountAllLinks(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                using (ApplicationContext db = new ApplicationContext())
                {
                    if (date == "/countLinks")
                    {
                        // Подсчитываем количество уникальных значений в столбце user_id
                        int count = db.UserLinks.Select(item => item.link).Count();

                        // Выводим результат
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: count.ToString(),
                            replyToMessageId: update.Message.MessageId
                        );
                    }
                    else
                    {
                        int uniqueUserCount = db.UserLinks
                        .Where(link => link.date == date) // Фильтруем по заданной дате
                        .Select(link => link.link)
                        .Count();

                        // Выводим результат
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: uniqueUserCount.ToString(),
                            replyToMessageId: update.Message.MessageId
                        );
                    }
                }
            }
        }
        public async Task CountBotLinks(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                using (ApplicationContext db = new ApplicationContext())
                {
                    if (date == "/countBotLinks")
                    {
                        // Подсчитываем количество уникальных значений в столбце user_id
                        int count = db.BotCommands.Select(item => item.command_type).Count();

                        // Выводим результат
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: count.ToString(),
                            replyToMessageId: update.Message.MessageId
                        );
                    }
                    else
                    {
                        int uniqueUserCount = db.BotCommands
                        .Where(link => link.date == date) // Фильтруем по заданной дате
                        .Select(link => link.command_type)
                        .Count();

                        // Выводим результат
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: uniqueUserCount.ToString(),
                            replyToMessageId: update.Message.MessageId
                        );
                    }
                }
            }
        }
        public async Task CountUniqUsers(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                if (date == "/uniqUsers")
                {
                    date = DateTime.Now.ToShortDateString();
                }
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Подсчитываем количество уникальных значений в столбце user_id
                    int uniqueUserCount = db.UserLinks
                        .Where(link => link.date == date) // Фильтруем по заданной дате
                        .Select(link => link.user_id)
                        .Distinct()
                        .Count();

                    // Выводим результат
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: uniqueUserCount.ToString(),
                        replyToMessageId: update.Message.MessageId
                    );
                }
            }
        }
        public async Task CountUniqChats(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                if (date == "/uniqChats")
                {
                    date = DateTime.Now.ToShortDateString();
                }
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Подсчитываем количество уникальных значений в столбце chat_id
                    int uniqueUserCount = db.UserLinks
                        .Where(link => link.date == date) // Фильтруем по заданной дате
                        .Select(link => link.chat_id)
                        .Distinct()
                        .Count();

                    // Выводим результат
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: uniqueUserCount.ToString(),
                        replyToMessageId: update.Message.MessageId
                    );
                }
            }
        }
        public async Task ServiceStatistic(string chatId, Update update, CancellationToken cancellationToken, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);

            if (chatId == jsonObjectAPI["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync (chatId, ChatAction.Typing);
                string statistic = null;
                using (ApplicationContext db = new ApplicationContext())
                {   
                    // Получаем все уникальные значения столбца link
                    var uniqueLinks = db.UserLinks.Select(link => link.link).Distinct().ToList();

                    // Подсчитываем количество каждого типа ссылок
                    foreach (var link in uniqueLinks)
                    {
                        int linkCount = db.UserLinks.Count(l => l.link == link);
                        statistic += $"\nКоличество {link}: {linkCount}";
                    }
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: statistic,
                    replyToMessageId: update.Message.MessageId
                    );
            }

        }
        public async Task ServiceBotStatistic(string chatId, Update update, CancellationToken cancellationToken, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);

            if (chatId == jsonObjectAPI["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                string statistic = null;
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Получаем все уникальные значения столбца link
                    var uniqueLinks = db.BotCommands.Select(link => link.command_type).Distinct().ToList();

                    // Подсчитываем количество каждого типа ссылок
                    foreach (var link in uniqueLinks)
                    {
                        int linkCount = db.BotCommands.Count(l => l.command_type == link);
                        statistic += $"\nКоличество {link}: {linkCount}";
                    }
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: statistic,
                    replyToMessageId: update.Message.MessageId
                    );
            }

        }
        public async Task ChatStatistic(string chatId, Update update, CancellationToken cancellationToken, TelegramBotClient botClient)
        {
            WebProxy torProxy = new WebProxy
            {
                Address = new Uri(torProxyUrl),
            };
            HttpClientHandler httpHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                Proxy = torProxy,
                UseCookies = false
            };
            HttpClient httpClient = new HttpClient(httpHandler);

            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);

            if (chatId == jsonObjectAPI["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                string statistic = null;
                int count = 0;
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Получаем все уникальные значения столбца chat_id и подсчитываем количество каждого
                    var chatIdStatistics = db.UserLinks
                        .GroupBy(link => link.chat_id)
                        .Select(group => new { ChatId = group.Key, Count = group.Count() })
                        .OrderByDescending(item => item.Count)
                        .ToList();

                    // Формируем строку статистики
                    foreach (var item in chatIdStatistics)
                    {
                        if (count < 10)
                        {
                            var url = "https://api.telegram.org/bot" + jsonObjectAPI["BotAPI"][0].ToString() + "/getChat?chat_id=" + item.ChatId;
                            var response = await httpClient.GetAsync(url);
                            var responseString = await response.Content.ReadAsStringAsync();

                            JObject jsonObject = JObject.Parse(responseString);
                            string userName = null;

                            if (jsonObject["result"] != null)
                            {
                                if (jsonObject["result"]?["username"] != null)
                                {
                                    userName = jsonObject["result"]["username"].ToString();
                                }
                                else
                                {
                                    if (jsonObject["result"]["title"] != null)
                                    {
                                        userName = jsonObject["result"]["title"].ToString();
                                    }
                                }
                            }
                            statistic += $"\n{item.ChatId} ({userName}): {item.Count}";
                            count++;
                        }
                    }
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: statistic,
                    replyToMessageId: update.Message.MessageId
                    );
            }

        }
        public async Task LanguageStatistics(string chatId, Update update, CancellationToken cancellationToken, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);

            if (chatId == jsonObjectAPI["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                string statistic = null;
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Получаем все уникальные значения столбца link
                    var uniqueLinks = db.UserLanguages.Select(link => link.language).Distinct().ToList();

                    // Подсчитываем количество каждого типа ссылок
                    foreach (var link in uniqueLinks)
                    {
                        int linkCount = db.UserLanguages.Count(l => l.language == link);
                        statistic += $"\nКоличество {link}: {linkCount}";
                    }
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: statistic,
                    replyToMessageId: update.Message.MessageId
                    );
            }

        }
        public async Task SendMsgToAllUsers(string chatId, TelegramBotClient botClient, Update update)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                if (chatId == jsonObjectAPI["AdminId"][0].ToString())
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                    string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";
                    string[] directories = Directory.GetDirectories(currentDirectory);

                    foreach (string userDirectory in directories)
                    {
                        string userId = userDirectory.Split("\\").Last();
                        Logs logs = new Logs(Convert.ToInt64(userId), 21312312, ":", "", "");

                        Language language = new Language("rand", "rand");
                        string lang = await language.GetCurrentLanguage(userId.ToString());
                        var url = "https://api.telegram.org/bot" + jsonObjectAPI["BotAPI"][0].ToString() + "/getChat?chat_id=" + userId;
                        var response = await client.GetAsync(url);
                        var responseString = await response.Content.ReadAsStringAsync();

                        JObject jsonObject = JObject.Parse(responseString);

                        if (jsonObject["ok"].ToString() == "True")
                        {
                            try
                            {
                                if (lang == "eng")
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: userId,
                                        text: "Duplicate video sending problem solved....\n\nRead more - https://t.me/cobainSaver/15"
                                    );
                                }
                                if (lang == "ukr")
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: userId,
                                        text: "Проблему з дубльованим надсиланням відео вирішено...\n\nДетальніше - https://t.me/cobainSaver/15"
                                    );
                                }
                                if (lang == "rus")
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId: userId,
                                        text: "Проблема с дублированием видео решена...\n\nПодробнее - https://t.me/cobainSaver/15"
                                    );
                                }
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
        public async Task SendAllRewies(string chatId, Update update, CancellationToken cancellationToken, string date, TelegramBotClient botClient)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                string marks = null;
                using (ApplicationContext db = new ApplicationContext())
                {
                    if (date == "/reviews")
                    {
                        date = DateTime.Now.ToShortDateString();
                    }
                    Dictionary<int, int> counts = new Dictionary<int, int>();

                    // Заполняем словарь нулями
                    for (int i = 0; i <= 4; i++)
                    {
                        counts[i] = 0;
                    }

                    // Получаем количество каждого значения mark из базы данных за заданную дату
                    var markCounts = db.UserReviews
                        .Where(ur => ur.date == date)
                        .GroupBy(ur => ur.mark)
                        .Select(g => new { Mark = g.Key, Count = g.Count() })
                        .ToList();

                    // Обновляем словарь с количеством значений mark
                    foreach (var item in markCounts)
                    {
                        if (counts.ContainsKey(item.Mark))
                        {
                            counts[item.Mark] = item.Count;
                        }
                    }

                    // Выводим количество значений mark
                    foreach (var kvp in counts)
                    {
                        //Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                        marks += $"\n{kvp.Key}: {kvp.Value}";
                    }
                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: marks);
            }
        }
        public async Task SendLastTenUsers(TelegramBotClient botClient, string chatId)
        {
            WebProxy torProxy = new WebProxy
            {
                Address = new Uri(torProxyUrl),
            };
            HttpClientHandler httpHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                Proxy = torProxy,
                UseCookies = false
            };
            HttpClient httpClient = new HttpClient(httpHandler);
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectCheck = JObject.Parse(jsonString);
            if (chatId == jsonObjectCheck["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                int count = 0;
                string currentDirectory = Directory.GetCurrentDirectory() + "\\ServerLogs";

                string[] userIds = System.IO.File.ReadAllLines(Path.Combine(currentDirectory, "list.txt"));
                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
                foreach (string userId in userIds)
                {
                    if (count < 10)
                    {
                        var url = "https://api.telegram.org/bot" + jsonObjectAPI["BotAPI"][0].ToString() + "/getChat?chat_id=" + userId;
                        var response = await httpClient.GetAsync(url);
                        var responseString = await response.Content.ReadAsStringAsync();

                        JObject jsonObject = JObject.Parse(responseString);
                        string userName = null;

                        if (jsonObject["result"] != null)
                        {
                            if (jsonObject["result"]?["username"] != null)
                            {
                                userName = jsonObject["result"]["username"].ToString();
                            }
                            else
                            {
                                if (jsonObject["result"]["title"] != null)
                                {
                                    userName = jsonObject["result"]["title"].ToString();
                                }
                            }
                        }

                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: userId + " " + $"({userName})"
                            , callbackData: "BackToYear" + " " + userId + " " + 0 + " " + chatId),
                        });
                        count++;
                    }
                    else
                    {
                        break;
                    }

                }
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Choose user"
                );
            }

        }
        public async Task SendServerLogs(string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                string currentDirectory = Directory.GetCurrentDirectory() + "\\ServerLogs";

                if (!Directory.Exists(currentDirectory))
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "There are no logs in your chat",
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "У вашому чаті немає логів",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "В вашем чате нет логов",
                            replyToMessageId: update.Message.MessageId);
                    }
                    return;
                }

                string file = "allServers.txt";

                string filePath = Path.Combine(currentDirectory, file);

                if (!System.IO.File.Exists(filePath))
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(chatId.ToString());
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "There are no logs for that date",
                            replyToMessageId: update.Message.MessageId
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Логів за цю дату немає",
                            replyToMessageId: update.Message.MessageId);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Логи за эту дату отсутствуют.",
                            replyToMessageId: update.Message.MessageId);
                    }
                    return;
                }
                else
                {
                    await using Stream stream = System.IO.File.OpenRead($"{filePath}");
                    await botClient.SendDocumentAsync(
                    chatId: chatId,
                        document: InputFile.FromStream(stream: stream, fileName: "allServers.txt"),
                        replyToMessageId: update.Message.MessageId
                        );
                    stream.Close();
                }
            }
        }
    }
}
