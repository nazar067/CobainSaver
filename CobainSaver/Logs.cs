using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using AngleSharp.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using VideoLibrary;
using Newtonsoft.Json.Linq;
using AngleSharp.Dom;
using static System.Net.WebRequestMethods;
using System.Net;

namespace CobainSaver
{
    internal class Logs
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
        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            Proxy = webProxy,
            UseCookies = false
        };
        private static readonly HttpClient client = new HttpClient(handler);


        public long ChatId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Msg { get; set; }
        public string ServerMsg { get; set; }
        public Logs(long ChatId, long UserId, string UserName, string Msg, string serverMsg)
        {
            this.ChatId = ChatId;
            this.UserId = UserId;
            this.UserName = UserName;
            this.Msg = Msg;
            this.ServerMsg = serverMsg;
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0");

        }
        public async Task WriteUserLogs()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string userFolderName = "UserLogs";
            string userFolderPath = Path.Combine(currentDirectory, userFolderName);

            string folderName = ChatId.ToString();
            string folderPath = Path.Combine(userFolderPath, folderName);
            if(!Directory.Exists(folderPath)) 
            {
                Directory.CreateDirectory(folderPath);
            }
            string lastFolderName = "logs";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                Directory.CreateDirectory(lastFolderPath);
            }
            string yearFolder = DateTime.Now.Year.ToString();
            string yearFolderPath = Path.Combine(lastFolderPath, yearFolder);
            if (!Directory.Exists(yearFolderPath))
            {
                Directory.CreateDirectory(yearFolderPath);
            }
            string monthFolder = DateTime.Now.Month.ToString();
            string monthFolderPath = Path.Combine(yearFolderPath, monthFolder);
            if (!Directory.Exists(monthFolderPath))
            {
                Directory.CreateDirectory(monthFolderPath);
            }

            string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
            string file = $"{currentDate}.txt";
            string filePath = Path.Combine(monthFolderPath, file);
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, DateTime.Now.ToLongTimeString() + ": " + UserName + "(" + UserId.ToString() + ")" + Msg);
            }
            else
            {
                System.IO.File.AppendAllText(filePath, $"{Environment.NewLine}{DateTime.Now.ToLongTimeString() + ": " + UserName + "(" + UserId.ToString() + ")" + Msg}");
            }
        }
        public async Task WriteServerLogs()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string serverFolderName = "ServerLogs";
            string serverFolderPath = Path.Combine(currentDirectory, serverFolderName);

            string allServers = "allServers.txt";
            string allFilePath = Path.Combine(serverFolderPath, allServers);
            if (!System.IO.File.Exists(allFilePath))
            {
                System.IO.File.WriteAllText(allFilePath, DateTime.Now.ToLongTimeString() + ": " + ServerMsg);
            }
            else
            {
                System.IO.File.AppendAllText(allFilePath, $"{Environment.NewLine}{DateTime.Now.ToLongTimeString() + ": " + ServerMsg}");
            }

            string folderName = ChatId.ToString();
            string folderPath = Path.Combine(serverFolderPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
            string file = $"{currentDate}.txt";
            string filePath = Path.Combine(folderPath, file);
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, DateTime.Now.ToLongTimeString() + ": " + ServerMsg);
            }
            else
            {
                System.IO.File.AppendAllText(filePath, $"{Environment.NewLine}{DateTime.Now.ToLongTimeString() + ": " + ServerMsg}");
            }
        }
        public async Task WriteUserReviews(string reviews, string pollId)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string userFolderName = "UserLogs";
            string userFolderPath = Path.Combine(currentDirectory, userFolderName);

            string folderName = ChatId.ToString();
            string folderPath = Path.Combine(userFolderPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string lastFolderName = "reviews";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                Directory.CreateDirectory(lastFolderPath);
            }
            string date = DateTime.Now.ToShortDateString();
            string file = $"{date}({pollId}).txt";
            string filePath = Path.Combine(lastFolderPath, file);

            string serverFolder = "ServerLogs";
            string serverFolderPath = Path.Combine(currentDirectory, serverFolder);

            string serverFolderName = "reviews";
            string srvFolderPath = Path.Combine(serverFolderPath, serverFolderName);
            if (!Directory.Exists(srvFolderPath))
            {
                Directory.CreateDirectory(srvFolderPath);
            }
            string srvFolderName = DateTime.Now.ToShortDateString();
            string srvLastFolderPath = Path.Combine(srvFolderPath, srvFolderName);
            if (!Directory.Exists(srvLastFolderPath))
            {
                Directory.CreateDirectory(srvLastFolderPath);
            }
            string serverFile = $"reviews.txt";
            string srvFilePath = Path.Combine(srvLastFolderPath, serverFile);
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath,
                        $"Yeah Im 100% satisfied!" + " " + $"{0}\n" +
                        $"Satisfied" + " " + $"{0}\n" +
                        $"Its fine" + " " + $"{0}\n" +
                        $"Unhappy" + " " + $"{0}\n" +
                        $"I didnt like it at all!" + " " + $"{0}\n");
            }
            else
            {
                System.IO.File.WriteAllText(filePath,
                        $"Yeah Im 100% satisfied!" + " " + $"{0}\n" +
                        $"Satisfied" + " " + $"{0}\n" +
                        $"Its fine" + " " + $"{0}\n" +
                        $"Unhappy" + " " + $"{0}\n" +
                        $"I didnt like it at all!" + " " + $"{0}\n");
            }
            if (!System.IO.File.Exists(srvFilePath))
            {
                System.IO.File.WriteAllText(srvFilePath,
                        $"Yeah Im 100% satisfied!" + " " + $"{0}\n" +
                        $"Satisfied" + " " + $"{0}\n" +
                        $"Its fine" + " " + $"{0}\n" +
                        $"Unhappy" + " " + $"{0}\n" +
                        $"I didnt like it at all!" + " " + $"{0}\n");
            }
            else
            {
                System.IO.File.WriteAllText(srvFilePath,
                        $"Yeah Im 100% satisfied!" + " " + $"{0}\n" +
                        $"Satisfied" + " " + $"{0}\n" +
                        $"Its fine" + " " + $"{0}\n" +
                        $"Unhappy" + " " + $"{0}\n" +
                        $"I didnt like it at all!" + " " + $"{0}\n");
            }
        }
        public async Task SendAllRewies(string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
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
        public async Task SendUserLogs(string year, string month, string date, string chatId, Update update, TelegramBotClient botClient, string chatToSend)
        {
            if(date == null)
            {
                return;
            }
            string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

            string folderName = chatId;

            string folderPath = Path.Combine(currentDirectory, folderName);
            string lastFolderName = "logs";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "There are no logs in your chat"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "У вашому чаті немає логів");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "В вашем чате нет логов");
                }
                return;
            }

            string yearFolder = year;
            string yearFolderPath = Path.Combine(lastFolderPath, yearFolder);
            if (!Directory.Exists(yearFolderPath))
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "There are no logs for that year"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логів за цей рік немає");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логов за этот год нету");
                }
                return;
            }

            string monthFolder = month;
            string monthFolderPath = Path.Combine(yearFolderPath, monthFolder);
            if (!Directory.Exists(yearFolderPath))
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "There are no logs for that year"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логів за цей рік немає");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логов за этот год нету");
                }
                return;
            }

            string file = $"{date}.txt";

            string filePath = Path.Combine(monthFolderPath, file);

            if (!System.IO.File.Exists(filePath))
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "There are no logs for that date"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логів за цю дату немає");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Логи за эту дату отсутствуют.");
                }
                return;
            }
            else
            {
                await using Stream stream = System.IO.File.OpenRead($"{filePath}");
                await botClient.SendDocumentAsync(
                    chatId: chatToSend,
                    document: InputFile.FromStream(stream: stream, fileName:$"logs {date}.txt")
                    );
                stream.Close();
            }
        }
        public async Task SendAllUsers(TelegramBotClient botClient, string chatId)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);
            string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

            string[] directories = Directory.GetDirectories(currentDirectory);
            var buttonsList = new List<InlineKeyboardButton[]>();
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
            foreach (string userDirectory in directories)
            {
                string userId = userDirectory.Split("\\").Last();
                var url = "https://api.telegram.org/bot" + jsonObjectAPI["BotAPI"][0].ToString() + "/getChat?chat_id=" + userId;
                var response = await client.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                JObject jsonObject = JObject.Parse(responseString);
                string userName = null;
                if(jsonObject["result"] != null)
                {
                    if (jsonObject["result"]?["username"] != null)
                    {
                        userName = jsonObject["result"]["username"].ToString();
                    }
                    else
                    {
                        if(jsonObject["result"]["title"] != null)
                            userName = jsonObject["result"]["title"].ToString();
                    }
                }
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: userId + " " + $"({userName})"
                    , callbackData: "BackToYear" + " " + userId + " " + 0 + " " + chatId),
                });
            }
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                replyMarkup: inlineKeyboard,
                text: "Choose user"
            );
        }
        public async Task SendAllYears(TelegramBotClient botClient, string chatId, int messageId, string chatToSend)
        {
            if(messageId == 0)
            {
                Message message = null;
                string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\logs";
                string[] directories = Directory.GetDirectories(directory);
                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    message = await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Choose year"
                    );
                }
                else if (lang == "ukr")
                {
                    message = await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Виберіть рік"
                    );
                }
                else if (lang == "rus")
                {
                    message = await botClient.SendTextMessageAsync(
                        chatId: chatToSend,
                        text: "Выберите год"
                    );
                }

                foreach (string userDirectory in directories)
                {
                    string year = userDirectory.Split("\\").Last();
                    buttonsList.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: year, callbackData: "Year" + " " + year + " " + chatId + " " + message.MessageId + " " + chatToSend),
                    });
                }

                if (lang == "eng")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: message.MessageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Choose year"
                    );
                }
                else if (lang == "ukr")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: message.MessageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Виберіть рік"
                    );
                }
                else if (lang == "rus")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: message.MessageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Выберите год"
                    );
                }
            }
            else
            {
                string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\logs";
                string[] directories = Directory.GetDirectories(directory);
                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                foreach (string userDirectory in directories)
                {
                    string year = userDirectory.Split("\\").Last();
                    buttonsList.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: year, callbackData: "Year" + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                });
                }

                if (lang == "eng")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: messageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Choose year"
                    );
                }
                else if (lang == "ukr")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: messageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Виберіть рік"
                    );
                }
                else if (lang == "rus")
                {
                    await botClient.EditMessageTextAsync(
                        messageId: messageId,
                        chatId: chatToSend,
                        replyMarkup: inlineKeyboard,
                        text: "Выберите год"
                    );
                }
            }
        }
        public async Task SendAllMonths(TelegramBotClient botClient, string chatId, string year, int messageId, string chatToSend)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());

            string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\logs" + $"\\{year}";
            string[] directories = Directory.GetDirectories(directory);
            var buttonsList = new List<InlineKeyboardButton[]>();
            if (lang == "eng")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Back", callbackData: "BackToYear" + " " + chatId + " " + messageId + " " + chatToSend),
                });
            }
            if (lang == "ukr")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "BackToYear" + " " + chatId + " " + messageId + " " + chatToSend),
                });
            }
            if (lang == "rus")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "BackToYear" + " " + chatId + " " + messageId + " " + chatToSend),
                });
            }
            foreach (string userDirectory in directories)
            {
                string month = userDirectory.Split("\\").Last();
                if(lang == "eng")
                {
                    if (month == "1")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "January", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "2")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "February", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "3")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "March", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "4")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "April", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "5")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "May", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "6")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "June", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "7")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "July", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "8")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "August", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "9")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "September", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "10")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "October", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "11")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "November", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "12")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "December", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                }
                if (lang == "ukr")
                {
                    if (month == "1")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Січень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "2")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Лютий", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "3")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Березень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "4")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Квітень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "5")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Травень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "6")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Червень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "7")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Липень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "8")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Серпень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "9")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Вересень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "10")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Жовтень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "11")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Листопад", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "12")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Грудень", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                }
                if (lang == "rus")
                {
                    if (month == "1")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Январь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "2")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Февраль", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "3")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Март", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "4")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Апрель", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "5")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Май", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "6")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Июнь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "7")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Июль", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "8")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Август", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "9")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Сентябрь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "10")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Октябрь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "11")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Ноябрь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                    if (month == "12")
                    {
                        buttonsList.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Декабрь", callbackData: "Month" + " " + month + " " + year + " " + chatId + " " + messageId + " " + chatToSend),
                        });
                    }
                }
            }
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
            if (lang == "eng")
            {
                await botClient.EditMessageTextAsync(
                    messageId: messageId, 
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Selected year: {year}\n" +
                    "Choose month"
                );
            }
            else if (lang == "ukr")
            {
                await botClient.EditMessageTextAsync(
                    messageId: messageId,
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Вибраний рік: {year}\n" +
                    "Виберіть місяць"
                );
            }
            else if (lang == "rus")
            {
                await botClient.EditMessageTextAsync(
                    messageId: messageId,
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Выбранный год: {year}\n" +
                    "Виберите месяц"
                );
            }
        }
        public async Task SendAllDates(TelegramBotClient botClient, string chatId, string year, string month, int messageId, string chatToSend)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());

            string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\logs" + $"\\{year}" + $"\\{month}";
            string[] files = Directory.GetFiles(directory);
            var buttonsList = new List<InlineKeyboardButton[]>();
            if (lang == "eng")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Back", callbackData: "BackToMonth" + " " + chatId + " " + year + " " + messageId + " " + chatToSend),
                });
            }
            if (lang == "ukr")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "BackToMonth" + " " + chatId + " " + year + " " + messageId + " " + chatToSend),
                });
            }
            if (lang == "rus")
            {
                buttonsList.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "BackToMonth" + " " + chatId + " " + year + " " + messageId + " " + chatToSend),
                });
            }
            foreach (string userFiles in files)
            {
                string dates = userFiles.Split("\\").Last();
                buttonsList.Add(new[]
                {
                        InlineKeyboardButton.WithCallbackData(text: dates, callbackData: "Date" + " " + chatId + " " + month + " " + dates + " " + year + " " + chatToSend),
                });
            }
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);

            string monthName = null;

            if (lang == "eng")
            {
                if(month == "1")
                    monthName = "January";
                if (month == "2")
                    monthName = "February";
                if (month == "3")
                    monthName = "March";
                if (month == "4")
                    monthName = "April";
                if (month == "5")
                    monthName = "May";
                if (month == "6")
                    monthName = "June";
                if (month == "7")
                    monthName = "July";
                if (month == "8")
                    monthName = "August";
                if (month == "9")
                    monthName = "September";
                if (month == "10")
                    monthName = "October";
                if (month == "11")
                    monthName = "November";
                if (month == "12")
                    monthName = "December";


                await botClient.EditMessageTextAsync(
                    messageId: messageId,
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Selected year: {year}\n" +
                    $"Selected month: {monthName}\n" +
                    "Choose date"
                );
            }
            else if (lang == "ukr")
            {
                if (month == "1")
                    monthName = "Січень";
                if (month == "2")
                    monthName = "Лютий";
                if (month == "3")
                    monthName = "Березень";
                if (month == "4")
                    monthName = "Квітень";
                if (month == "5")
                    monthName = "Травень";
                if (month == "6")
                    monthName = "Червень";
                if (month == "7")
                    monthName = "Липень";
                if (month == "8")
                    monthName = "Серпень";
                if (month == "9")
                    monthName = "Вересень";
                if (month == "10")
                    monthName = "Жовтень";
                if (month == "11")
                    monthName = "Листопад";
                if (month == "12")
                    monthName = "Грудень";

                await botClient.EditMessageTextAsync(
                    messageId: messageId,
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Вибранний рік: {year}\n" +
                    $"Вибранний місяць: {monthName}\n" +
                    "Виберіть дату"
                );
            }
            else if (lang == "rus")
            {
                if (month == "1")
                    monthName = "Январь";
                if (month == "2")
                    monthName = "Февраль";
                if (month == "3")
                    monthName = "Март";
                if (month == "4")
                    monthName = "Апрель";
                if (month == "5")
                    monthName = "Май";
                if (month == "6")
                    monthName = "Июнь";
                if (month == "7")
                    monthName = "Июль";
                if (month == "8")
                    monthName = "Август";
                if (month == "9")
                    monthName = "Сентябрь";
                if (month == "10")
                    monthName = "Октябрь";
                if (month == "11")
                    monthName = "Ноябрь";
                if (month == "12")
                    monthName = "Декабрь";

                await botClient.EditMessageTextAsync(
                    messageId: messageId,
                    chatId: chatToSend,
                    replyMarkup: inlineKeyboard,
                    text: $"Выбранный год: {year}\n" +
                    $"Выбранный месяц: {monthName} \n" +
                    "Виберите дату"
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
        public async Task SendUserLogsToAdmin(string userId, string date, string chatId, Update update, CancellationToken cancellationToken, Message message, TelegramBotClient botClient, string cobain)
        {
            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObject = JObject.Parse(jsonString);
            if (chatId == jsonObject["AdminId"][0].ToString())
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                await SendAllUsers(botClient, chatId);
            }
        }
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

            }
        }
    }
}
