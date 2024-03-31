using CobainSaver.DataBase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoLibrary;
using YoutubeExplode.Channels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CobainSaver
{
    internal class Language
    {
        public string Lang { get; set; }
        public string Message { get; set; }
        public Language(string lang, string message)
        {
            this.Lang = lang;
            Message = message;

        }

        public async Task ChangeLanguage(string chatId, TelegramBotClient botClient)
        {
            AddToDataBase addDB = new AddToDataBase();
            await addDB.AddUserLanguage(Convert.ToInt64(chatId), Lang);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: Message);
        }
        public async Task StartLanguage(string chatId, TelegramBotClient botClient)
        {
            AddToDataBase addDB = new AddToDataBase();
            await addDB.AddUserLanguage(Convert.ToInt64(chatId), Lang);
        }
        public async Task<string> GetCurrentLanguage(string chatId)
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                // Выполняем запрос к базе данных для получения записи с заданным chat_id
                var userLanguage = db.UserLanguages.FirstOrDefault(ul => ul.chat_id == Convert.ToInt64(chatId));
                if (userLanguage != null) 
                {
                    if (userLanguage.language == null)
                    {
                        return "eng";
                    }
                    else if (userLanguage.language == "en")
                    {
                        return "eng";
                    }
                    else if (userLanguage.language == "uk")
                    {
                        return "ukr";
                    }
                    else if (userLanguage.language == "ru")
                    {
                        return "rus";
                    }
                    else
                    {
                        return "eng";
                    }
                }
                return "eng";
            }
        }
        public async Task ChangeLanguageAllUsers(string chatId, TelegramBotClient botClient)
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText("source.json");
                JObject jsonObjectAPI = JObject.Parse(jsonString);

                if (chatId == jsonObjectAPI["AdminId"][0].ToString())
                {
                    await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
                    string currentDirectory = Directory.GetCurrentDirectory();

                    string userFolderName = "UserLogs";
                    string userLogsDirectory = Path.Combine(currentDirectory, userFolderName);

                    string lang = "en";

                    // Проверяем существует ли указанная директория
                    if (Directory.Exists(userLogsDirectory))
                    {
                        // Получаем все подкаталоги в указанной директории
                        string[] userDirectories = Directory.GetDirectories(userLogsDirectory);

                        // Перебираем каждый подкаталог
                        foreach (string userDirectory in userDirectories)
                        {
                            try
                            {
                                // Получаем все файлы внутри подкаталога
                                string[] files = Directory.GetFiles(Path.Combine(userDirectory, "serv"));

                                // Выводим только имя последнего файла
                                if (files.Length > 0)
                                {
                                    string lastFileName = Path.GetFileName(files[files.Length - 1]);
                                    lang = lastFileName;
                                }
                                if(lang == "rus.txt")
                                {
                                    Lang = "ru";
                                }
                                else if (lang == "eng.txt")
                                {
                                    Lang = "en";
                                }
                                else if (lang == "ukr.txt")
                                {
                                    Lang = "uk";
                                }
                                string chat_id = Path.GetFileName(userDirectory);
                                await StartLanguage(chat_id, botClient);
                            }
                            catch (Exception ex) 
                            {
                                await Console.Out.WriteLineAsync(ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Указанная директория UserLogs не существует.");
                    }
                }
            }
            catch (Exception ex)
            {
                //await Console.Out.WriteLineAsync(ex.ToString());
                /*                try
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
                                }*/
            }
        }
    }
}
