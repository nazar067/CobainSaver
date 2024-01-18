using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

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

            string currentDate = DateTime.Now.ToString("dd-MM-yyyy");
            string file = $"{currentDate}.txt";
            string filePath = Path.Combine(folderPath, file);
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
        public async Task SendUserLogs(string date, string chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            if(date == "/logs")
            {
                date = DateTime.Now.ToString("dd-MM-yyyy");
            }
            if (date.Contains("/"))
            {
                date = date.Replace('.', '-');
            }
            if(date.Contains("."))
            {
                date = date.Replace('.', '-');
            }
            string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";

            string folderName = chatId;

            string folderPath = Path.Combine(currentDirectory, folderName);
            if (!Directory.Exists(folderPath))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "There are no logs in your chat",
                    replyToMessageId: update.Message.MessageId
                    );
                return;
            }

            string file = $"{date}.txt";

            string filePath = Path.Combine(folderPath, file);

            if (!System.IO.File.Exists(filePath))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "There are no logs for that date",
                    replyToMessageId: update.Message.MessageId
                    );
                return;
            }
            else
            {
                await using Stream stream = System.IO.File.OpenRead($"{filePath}");
                await botClient.SendDocumentAsync(
                    chatId: chatId,
                    document: InputFile.FromStream(stream: stream, fileName:"logs.txt(" + date + ")"),
                    replyToMessageId: update.Message.MessageId
                    );
                stream.Close();
            }
        }
    }
}
