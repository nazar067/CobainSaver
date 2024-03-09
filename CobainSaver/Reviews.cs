using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using VideoLibrary;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CobainSaver
{
    internal class Reviews
    {
        private static readonly HttpClient client = new HttpClient();
        public async Task UserReviews(string chatId, TelegramBotClient botClient)
        {
            bool check = await CheckIsFile(chatId);
            if(DateTime.Now.Day == 18 && check == true)
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                Message pollMessage = null;

                string currentDirectory = Directory.GetCurrentDirectory() + "\\UserLogs";
                string[] directories = Directory.GetDirectories(currentDirectory);

                foreach (string userDirectory in directories)
                {
                    string userId = userDirectory.Split("\\").Last();
                    Logs logs = new Logs(Convert.ToInt64(userId), 21312312, ":", "", "");

                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(userId.ToString());
                    bool checkUser = await CheckIsFile(userId);
                    var url = "https://api.telegram.org/bot" + jsonObjectAPI["BotAPI"][0].ToString() + "/getChat?chat_id=" + userId;
                    var response = await client.GetAsync(url);
                    var responseString = await response.Content.ReadAsStringAsync();

                    JObject jsonObject = JObject.Parse(responseString);

                    if (jsonObject["ok"].ToString() == "True")
                    {
                        try
                        {
                            if (lang == "eng" && checkUser == true)
                            {
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: userId,
                                        isAnonymous: false,
                                        question: "Are you satisfied with the quality of cobainSaver's service for this month?" +
                                        " Your opinion is important to us!" +
                                        " The survey will only last 1 day, then the results will not be counted",
                                        options: new[]
                                        {
                                        "Yeah, I'm 100% satisfied!",
                                        "Satisfied",
                                        "It's fine",
                                        "Unhappy",
                                        "I didn't like it at all!"
                                        });
                                await logs.WriteUserReviews(pollMessage.Poll.Question + " " + pollMessage.MessageId, pollMessage.Poll.Id);
                                await botClient.PinChatMessageAsync(userId, pollMessage.MessageId);
                            }
                            if (lang == "ukr" && checkUser == true)
                            {
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: userId,
                                        isAnonymous: false,
                                        question: "Наскільки ви задоволені якістю бота цього місяця?" +
                                        " Ваша думка важлива для нас!" +
                                        " Опитування триватиме лише 1 день, потім результати не враховуватимуться",
                                        options: new[]
                                        {
                                        "Я на 100% задоволений!",
                                        "Задоволений",
                                        "Нормально",
                                        "Незадоволений",
                                        "Взгалі не подобається!"
                                        });
                                await logs.WriteUserReviews(pollMessage.Poll.Question + " " + pollMessage.MessageId, pollMessage.Poll.Id);
                                await botClient.PinChatMessageAsync(userId, pollMessage.MessageId);
                            }
                            if (lang == "rus" && checkUser == true)
                            {
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: userId,
                                        isAnonymous: false,
                                        question: "Насколько вы довольны качеством бота в этом месяце?" +
                                        " Ваше мнение важно для нас!" +
                                        " Опрос будет длится только 1 день, затем результаты не буду учитываться",
                                        options: new[]
                                        {
                                        "Я на 100% доволен!",
                                        "Доволен",
                                        "Нормально",
                                        "Недоволен",
                                        "Вообще не нравится!"
                                        });
                                await logs.WriteUserReviews(pollMessage.Poll.Question + " " + pollMessage.MessageId, pollMessage.Poll.Id);
                                await botClient.PinChatMessageAsync(userId, pollMessage.MessageId);
                            }
                        }
                        catch (Exception e) 
                        {
                        }
                    }
                }
            }
            
        }
        public async Task LogsUserReviews(string pollId, 
            int firstVote, int secondVote, int thirdVote, int fourthVote, int fifthVote, string userId)
        {
            string correctDir = null;
            string directory = Directory.GetCurrentDirectory() + "\\UserLogs";
            string[] directories = System.IO.Directory.GetDirectories(directory);
            string fileName = $"{DateTime.Now.ToShortDateString()}({pollId}).txt";
            foreach (string dir in directories)
            {
                string[] underDir = System.IO.Directory.GetDirectories(dir);
                foreach (string d in underDir)
                {
                    string[] f = Directory.GetFiles(d, fileName);

                    // Выводим найденные файлы
                    foreach (string file in f)
                    {
                        correctDir = file;
                    }

                }
            }
            if(correctDir == null)
            {
                return;
            }
            string filePath = Path.Combine(directory, correctDir);
            string[] userIds = System.IO.File.ReadAllLines(filePath);

            // Проверяем каждую строку
            foreach (string line in userIds)
            {
                if (line == userId)
                {
                    return;
                }
            }
            string[] lines = System.IO.File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++) 
            {
                if (lines[i].Contains("Yeah"))
                {
                    int oldValue = Convert.ToInt32(lines[i].Substring(lines[i].Length - 1));
                    int newValue = oldValue + firstVote; // Вычисляем новое значение
                    lines[i] = $"Yeah Im 100% satisfied! {newValue}"; // Обновляем строку
                }
                else if (lines[i].Contains("Satisfied"))
                {
                    // Аналогично обновляем строки для остальных переменных
                    int oldValue = Convert.ToInt32(lines[i].Substring(lines[i].Length - 1));
                    int newValue = oldValue + secondVote;
                    lines[i] = $"Satisfied {newValue}";
                }
                else if (lines[i].Contains("Its fine"))
                {
                    // Аналогично обновляем строки для остальных переменных
                    int oldValue = Convert.ToInt32(lines[i].Substring(lines[i].Length - 1));
                    int newValue = oldValue + thirdVote;
                    lines[i] = $"Its fine {newValue}";
                }
                else if (lines[i].Contains("Unhappy"))
                {
                    // Аналогично обновляем строки для остальных переменных
                    int oldValue = Convert.ToInt32(lines[i].Substring(lines[i].Length - 1));
                    int newValue = oldValue + fourthVote;
                    lines[i] = $"Unhappy {newValue}";
                }
                else if (lines[i].Contains("I didnt like it at all!"))
                {
                    // Аналогично обновляем строки для остальных переменных
                    int oldValue = Convert.ToInt32(lines[i].Substring(lines[i].Length - 1));
                    int newValue = oldValue + fifthVote;
                    lines[i] = $"I didnt like it at all! {newValue}";
                }
                System.IO.File.WriteAllLines(filePath, lines);
                using (StreamWriter writer = System.IO.File.AppendText(filePath))
                {
                    writer.WriteLine(userId);
                }

            }
        }
        public async Task<bool> CheckIsFile(string chatId)
        {
            string fileName = null;
            string directory = Directory.GetCurrentDirectory() + "\\UserLogs\\" + chatId + "\\reviews";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string[] files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                fileName = file;
                if (fileName.Contains(DateTime.Now.ToShortDateString()))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
