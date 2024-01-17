using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, DateTime.Now.ToLongTimeString() + ": " + UserName + "(" + UserId.ToString() + ")" + Msg);
            }
            else
            {
                File.AppendAllText(filePath, $"{Environment.NewLine}{DateTime.Now.ToLongTimeString() + ": " + UserName + "(" + UserId.ToString() + ")" + Msg}");
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
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, DateTime.Now.ToLongTimeString() + ": " + ServerMsg);
            }
            else
            {
                File.AppendAllText(filePath, $"{Environment.NewLine}{DateTime.Now.ToLongTimeString() + ": " + ServerMsg}");
            }
        }
    }
}
