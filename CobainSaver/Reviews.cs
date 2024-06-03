using CobainSaver.DataBase;
using Microsoft.EntityFrameworkCore;
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
        public async Task UserReviews(string chatId, TelegramBotClient botClient)
        {
            bool check = await GlobalCheck(DateTime.Now.ToShortDateString());
            if(DateTime.Now.Day == 03 && check == true)
            {
                AddToDataBase addDB = new AddToDataBase();

                Message pollMessage = null;

                using(var db = new ApplicationContext())
                {

                    List<long> chat_ids = await db.UserLinks
                                                .Select(ul => ul.chat_id)
                                                .Distinct()
                                                .ToListAsync();

                    foreach (var chat_id in chat_ids)
                    {
                        try
                        {
                            Language language = new Language("rand", "rand");
                            string lang = await language.GetCurrentLanguage(chat_id.ToString());
                            bool checkUser = await CheckIsFile(chat_id.ToString(), DateTime.Now.ToShortDateString());
                            if (lang == "eng" && checkUser == true)
                            {
                                await addDB.AddChatReviews(Convert.ToInt64(chat_id), DateTime.Now.ToShortDateString());
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: chat_id,
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
                                await botClient.SendTextMessageAsync(
                                    chatId: chat_id,
                                    text: "If you have any problems or suggestions, you can share them in our channel - t.me/cobainSaver"
                                );
                                await botClient.PinChatMessageAsync(chat_id, pollMessage.MessageId);
                            }
                            if (lang == "ukr" && checkUser == true)
                            {
                                await addDB.AddChatReviews(Convert.ToInt64(chat_id), DateTime.Now.ToShortDateString());
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: chat_id,
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
                                await botClient.SendTextMessageAsync(
                                    chatId: chat_id,
                                    text: "Якщо у вас є проблеми або пропозиції, ви можете ними поділитися в нашому каналі - t.me/cobainSaver"
                                );
                                await botClient.PinChatMessageAsync(chat_id, pollMessage.MessageId);
                            }
                            if (lang == "rus" && checkUser == true)
                            {
                                await addDB.AddChatReviews(Convert.ToInt64(chat_id), DateTime.Now.ToShortDateString());
                                pollMessage = await botClient.SendPollAsync(
                                        chatId: chat_id,
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
                                await botClient.SendTextMessageAsync(
                                    chatId: chat_id,
                                    text: "Если у вас есть проблемы или предложения, вы можете ими поделиться в нашем канале - t.me/cobainSaver"
                                );
                                await botClient.PinChatMessageAsync(chat_id, pollMessage.MessageId);
                            }
                        }
                        catch (Exception e) 
                        {
                        }
                        await Task.Delay(2000);
                    }
                }

                
            }
            
        }
        public async Task LogsUserReviews(string pollId, int mark, string userId)
        {
            AddToDataBase addDB = new AddToDataBase();
            await addDB.AddUserReviews(Convert.ToInt64(userId), Convert.ToInt64(userId), mark, DateTime.Now.ToShortDateString());
        }
        public async Task<bool> CheckIsFile(string chatId, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    // Выполняем LINQ запрос для поиска записи с заданными chat_id и date
                    var check = db.ChatReviews
                        .FirstOrDefault(chat => chat.chat_id == Convert.ToInt64(chatId) && chat.date == date);


                    if (check != null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch(Exception e)
            {
                //await Console.Out.WriteLineAsync(e.ToString());
                if(e.ToString().StartsWith("Npgsql.PostgresException (0x80004005):"))
                    return true;
                return false;
            }
        }
        public async Task<bool> GlobalCheck(string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    var check = db.ChatReviews
                        .FirstOrDefault(chat => chat.date == date);


                    if (check != null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                //await Console.Out.WriteLineAsync(e.ToString());
                return false;
            }
        }
    }
}
