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

namespace CobainSaver
{
    internal class Logs
    {
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

            string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
            string file = $"{currentDate}.txt";
            string filePath = Path.Combine(lastFolderPath, file);
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
        }
        public async Task SendUserLogs(string date, string chatId, Update update, TelegramBotClient botClient)
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
                        chatId: chatId,
                        text: "There are no logs in your chat"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вашому чаті немає логів");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "В вашем чате нет логов");
                }
                return;
            }

            string file = $"{date}.txt";

            string filePath = Path.Combine(lastFolderPath, file);

            if (!System.IO.File.Exists(filePath))
            {
                Language language = new Language("rand", "rand");
                string lang = await language.GetCurrentLanguage(chatId.ToString());
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "There are no logs for that date"
                        );
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Логів за цю дату немає");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Логи за эту дату отсутствуют.");
                }
                return;
            }
            else
            {
                await using Stream stream = System.IO.File.OpenRead($"{filePath}");
                await botClient.SendDocumentAsync(
                    chatId: chatId,
                    document: InputFile.FromStream(stream: stream, fileName:"logs.txt(" + date + ")")
                    );
                stream.Close();
            }
        }
        public async Task SendAllDates(TelegramBotClient botClient, string chatId)
        {
            string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{chatId}" + $"\\logs";
            string[] files = Directory.GetFiles(directory);
            var buttonsList = new List<InlineKeyboardButton[]>();
            foreach (string userFile in files)
            {
                string id = userFile.Split("\\").Last();
                buttonsList.Add(new[]
                {
                        InlineKeyboardButton.WithCallbackData(text: id, callbackData: id + " " + chatId),
                });
            }
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());
            if (lang == "eng")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Choose date"
                );
            }
            else if(lang == "ukr")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Виберіть дату"
                );
            }
            else if (lang == "rus")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Выберите дату"
                );
            }
        }
        public async Task SendServerLogs(string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            if(chatId == "Admin id")
            {
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
        public async Task SendUserLogsToAdmin(string userId, string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            if(chatId == "Admin id")
            {
                if (userId == "/userLogs")
                {
                    string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

                    string[] directories = Directory.GetDirectories(currentDirectory);

                    foreach (var directory in directories)
                    {
                        string id = directory.Split("\\").Last();
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: id
                            );
                    }
                }
                else
                {
                    string[] words = userId.Split(' ');
                    userId = words[1];

                    if (date.StartsWith("/userLogs "))
                    {
                        string directory = Directory.GetCurrentDirectory() + "\\UserLogs" + $"\\{userId}" + $"\\logs";
                        string[] files = Directory.GetFiles(directory);
                        foreach (var userFile in files)
                        {
                            string id = userFile.Split("\\").Last();
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: id
                                );
                        }
                        return;
                    }
                    if (date.Contains("/"))
                    {
                        date = date.Replace('.', '-');
                    }
                    if (date.Contains("."))
                    {
                        date = date.Replace('.', '-');
                    }
                    string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

                    string folderName = userId;

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

                    string file = $"{date}.txt";

                    string filePath = Path.Combine(lastFolderPath, file);

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
                            document: InputFile.FromStream(stream: stream, fileName: "logs.txt(" + date + ")"),
                            replyToMessageId: update.Message.MessageId
                            );
                        stream.Close();
                    }
                }
            }
        }
        public async Task CountAllUsers(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient, string cobain)
        {
            if (chatId == "Admin id")
            {
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
