using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Telegram.Bot.Types;

namespace CobainSaver.DataBase
{
    internal class AddToDataBase
    {
        public async Task AddUserLinks(long chatId, long userId, string link, long msg_id, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    UserLink userLink = new UserLink
                    {
                        chat_id = chatId,
                        user_id = userId,
                        link = link,
                        msg_id = msg_id,
                        date = date,
                    };
                    db.UserLinks.Add(userLink);
                    db.SaveChanges();
                } 
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, userId, "username", link, ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }

        }
        public async Task AddBotCommands(long chatId, string commandType, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    BotCommand botCommand = new BotCommand
                    {
                        chat_id = chatId,
                        command_type = commandType,
                        date = date
                    };
                    db.BotCommands.Add(botCommand);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddBotCommands", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddUserCommands(long chatId, long userId, string command, long msgId, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    UserCommand userCommand = new UserCommand
                    {
                        user_id = userId,
                        chat_id = chatId,
                        command = command,
                        msg_id = msgId,
                        date = date,
                    };
                    db.UserCommands.Add(userCommand);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddUserCommands", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddUserReviews(long chatId, long userId, int mark, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    string currentDate = DateTime.Now.ToShortDateString();

                    // Проверяем наличие записи с заданным userId и текущей датой
                    bool reviewExists = await db.UserReviews.AnyAsync(ur => ur.user_id == userId && ur.date == currentDate);

                    if (!reviewExists)
                    {
                        UserReview userReview = new UserReview
                        {
                            chat_id = chatId,
                            user_id = userId,
                            mark = mark,
                            date = currentDate.ToString() // Преобразуем текущую дату в строку
                        };
                        db.UserReviews.Add(userReview);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddUserReviews", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddUserLanguage(long chatId, string lng_code)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    var existingRecord = db.UserLanguages.FirstOrDefault(ul => ul.chat_id == chatId);

                    if (existingRecord != null)
                    {
                        // Если запись существует, обновляем ее
                        existingRecord.language = lng_code;
                        db.SaveChanges();
                    }
                    else
                    {
                        // Если записи с таким chat_id не существует, добавляем новую запись
                        UserLanguage userLanguage = new UserLanguage
                        {
                            chat_id = chatId,
                            language = lng_code
                        };
                        db.UserLanguages.Add(userLanguage);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddUserLanguage", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddChatReviews(long chatId, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    ChatReview chatReview = new ChatReview
                    {
                        chat_id = chatId,
                        date = date
                    };
                    db.ChatReviews.Add(chatReview);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddChatReview", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddAds(int id, long chatId, string adsName, string message, bool isActive, bool isActiveAdmin, string startDate, string endDate)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    var existingRecord = db.AdsProfiles.FirstOrDefault(ul => ul.Id == id);

                    if (existingRecord != null)
                    {
                        // Если запись существует, обновляем ее
                        existingRecord.ads_name = adsName;
                        existingRecord.message = message;
                        existingRecord.is_active = isActive;
                        existingRecord.is_activeAdmin = isActiveAdmin;
                        existingRecord.end_date = endDate;
                        db.SaveChanges();
                    }
                    else
                    {
                        // Если записи с таким chat_id не существует, добавляем новую запись
                        AdsProfile adsProfile = new AdsProfile
                        {
                            chat_id = chatId,
                            ads_name = adsName,
                            message = message,
                            is_active = isActive,
                            is_activeAdmin = isActiveAdmin,
                            start_date = startDate,
                            end_date = endDate
                        };
                        db.AdsProfiles.Add(adsProfile);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "AddUserLanguage", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task DeleteAds(int id, long chatId)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    var ads = await db.AdsProfiles.FindAsync(id);

                    if (ads != null)
                    {
                        db.AdsProfiles.Remove(ads);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(chatId, 0, "username", "DeleteAds", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
        public async Task AddUserDonates(long userId, int stars, string date)
        {
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    string currentDate = DateTime.Now.ToShortDateString();

                    UserDonate userReview = new UserDonate
                    {
                        user_id = userId,
                        stars = stars,
                        date = currentDate.ToString() // Преобразуем текущую дату в строку
                    };
                    db.UserDonates.Add(userReview);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logs logs = new Logs(userId, 0, "username", "AddUserDonates", ex.ToString());
                    await logs.WriteServerLogs();
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
    }
}
