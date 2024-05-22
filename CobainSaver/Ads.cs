using CobainSaver.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using YoutubeExplode.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CobainSaver
{
    internal class Ads
    {
        public async Task SendAllAds(TelegramBotClient botClient, long chatId)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());
            using (ApplicationContext db = new ApplicationContext()) // Замените на ваш контекст базы данных
            {
                // Проверяем наличие входящего id в столбце chat_id
                if(!db.AdsProfiles.Any(x => x.chat_id == chatId))
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You don't have an ad yet, contact @nazar067 to purchase one.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас ще немає реклами, зверніться до @nazar067, щоб її придбати.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас еще нету рекламы, обратитесь к @nazar067, чтобы ее приобрести.");
                    }
                    return;
                }
                var adsProfiles = await db.AdsProfiles.Where(profile => profile.chat_id == chatId).ToListAsync();
                var buttonsList = new List<InlineKeyboardButton[]>();
                InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(buttonsList);
                foreach (var profile in adsProfiles)
                {
                    buttonsList.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: profile.ads_name, 
                        callbackData: "Ad"+ " " + profile.Id.ToString()),
                    });
                }
                if (lang == "eng")
                {
                    await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Choose ad");
                }
                if (lang == "ukr")
                {
                    await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Виберіть рекламу");
                }
                if (lang == "rus")
                {
                    await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyMarkup: inlineKeyboard,
                    text: "Выберите рекламу");
                }
            }
        }
        public async Task SendAdDesc(TelegramBotClient botClient, int id)
        {
            using (ApplicationContext db = new ApplicationContext()) // Замените на ваш контекст базы данных
            {
                var adsProfiles = await db.AdsProfiles.Where(profile => profile.Id == id).ToListAsync();
                foreach (var profile in adsProfiles)
                {
                    Language language = new Language("rand", "rand");
                    string lang = await language.GetCurrentLanguage(profile.chat_id.ToString());
                    if(lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Id:" + profile.Id + "\nName: " + profile.ads_name + "\nText: " + profile.message + "\nIs active: " + profile.is_active + "\nStart date: " + profile.start_date + "\nEnd date: " + profile.end_date + "\n\n" +
                            "<b>Change the name (only you can see it)</b> \n/adsEditName ad's id new name, example /adsEditName 1 Rozetka\n\n" +
                            "<b>Change description</b> \n/adsEditDesc ad's id new description, example /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Enable/disable</b> \n/adsEditActive ad's id True/False, example /adsEditActive 1 True\n\n",
                            parseMode: ParseMode.Html);
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Id:" + profile.Id + "\nНазва: " + profile.ads_name + "\nОпис: " + profile.message + "\nАктивна: " + profile.is_active + "\nПочаткова дата: " + profile.start_date + "\nКінцева дата: " + profile.end_date + "\n\n" +
                            "<b>Змінити назву(тільки ви бачите її)</b> \n/adsEditName ad's id new name, наприклад /adsEditName 1 Rozetka\n\n" +
                            "<b>Змінити опис</b> \n/adsEditDesc ad's id new description, наприклад /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Активувати/деактивувати</b> \n/adsEditActive ad's id True/False, наприклад /adsEditActive 1 True\n\n",
                            parseMode: ParseMode.Html);
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Id:" + profile.Id + "\nИмя: " + profile.ads_name + "\nОписание: " + profile.message + "\nАктивна: " + profile.is_active + "\nНачальная дата: " + profile.start_date + "\nКонечная дата: " + profile.end_date + "\n\n" +
                            "<b>Изменить название(только вы его видите)</b> \n/adsEditName ad's id new name, например /adsEditName 1 Rozetka\n\n" +
                            "<b>Изменить описание</b> \n/adsEditDesc ad's id new description, например /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Активировать/деактивировать</b> \n/adsEditActive ad's id True/False, example /adsEditActive 1 True\n\n",
                            parseMode: ParseMode.Html);
                    }
                }
            }
        }
        public async Task EditAdName(TelegramBotClient botClient, int id, string name, long chatId)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());
            using (ApplicationContext db = new ApplicationContext())
            {
                if (!db.AdsProfiles.Any(x => x.chat_id == chatId))
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You don't have an ad yet, contact @nazar067 to purchase one.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас ще немає реклами, зверніться до @nazar067, щоб її придбати.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас еще нету рекламы, обратитесь к @nazar067, чтобы ее приобрести.");
                    }
                    return;
                }
                AddToDataBase add = new AddToDataBase();
                var adsProfile = await db.AdsProfiles.Where(profile => profile.Id == id && profile.chat_id == chatId).ToListAsync();
                if(adsProfile == null)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "No advertisements were found with this Id.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Реклами з таким Id не знайдено.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Рекламы с таким Id не найдено.");
                    }
                    return;
                }
                foreach (var profile in adsProfile)
                {
                    await add.AddAds(id, profile.chat_id, name, profile.message, profile.is_active, profile.is_activeAdmin, profile.start_date, profile.end_date);
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Success update."
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успішно оновлено."
                            );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успешно обновлено."
                            );
                    }
                }
            }
        }
        public async Task EditAdDescription(TelegramBotClient botClient, int id, string message, long chatId, Message msg)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());
            using (ApplicationContext db = new ApplicationContext())
            {
                if (!db.AdsProfiles.Any(x => x.chat_id == chatId))
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You don't have an ad yet, contact @nazar067 to purchase one.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас ще немає реклами, зверніться до @nazar067, щоб її придбати.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас еще нету рекламы, обратитесь к @nazar067, чтобы ее приобрести.");
                    }
                    return;
                }
                if(message.Length > 100)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "The description is too large, it should be 100 characters or less.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Опис занадто великий, він має бути не більше 100 символів.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Описание слишком большое, оно должно быть не более 100 символов.");
                    }
                    return;
                }
                AddToDataBase add = new AddToDataBase();
                var adsProfile = await db.AdsProfiles.Where(profile => profile.Id == id && profile.chat_id == chatId).ToListAsync();
                if (adsProfile == null)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "No advertisements were found with this Id.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Реклами з таким Id не знайдено.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Рекламы с таким Id не найдено.");
                    }
                    return;
                }
                string formatedMessage = GetParseMode(msg, message);


                foreach (var profile in adsProfile)
                {
                    await add.AddAds(id, profile.chat_id, profile.ads_name, formatedMessage, profile.is_active, profile.is_activeAdmin, profile.start_date, profile.end_date);
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Success update."
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успішно оновлено."
                            );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успешно обновлено."
                            );
                    }
                }
            }
        }
        public async Task EditAdActive(TelegramBotClient botClient, int id, bool isActive, long chatId)
        {
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());
            using (ApplicationContext db = new ApplicationContext())
            {
                if (!db.AdsProfiles.Any(x => x.chat_id == chatId))
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You don't have an ad yet, contact @nazar067 to purchase one.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас ще немає реклами, зверніться до @nazar067, щоб її придбати.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас еще нету рекламы, обратитесь к @nazar067, чтобы ее приобрести.");
                    }
                    return;
                }
                AddToDataBase add = new AddToDataBase();
                var adsProfile = await db.AdsProfiles.Where(profile => profile.Id == id && profile.chat_id == chatId).ToListAsync();
                if (adsProfile == null)
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "No advertisements were found with this Id.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Реклами з таким Id не знайдено.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Рекламы с таким Id не найдено.");
                    }
                    return;
                }
                foreach (var profile in adsProfile)
                {
                    await add.AddAds(id, profile.chat_id, profile.ads_name, profile.message, isActive, profile.is_activeAdmin, profile.start_date, profile.end_date);
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Success update."
                            );
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успішно оновлено."
                            );
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: profile.chat_id,
                            text: "Успешно обновлено."
                            );
                    }
                }
            }
        }
        public async Task AdsHelp(TelegramBotClient botClient, long chatId, Telegram.Bot.Types.Update update)
        {
            await botClient.SendChatActionAsync(chatId, ChatAction.Typing);
            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId.ToString());

            using (ApplicationContext db = new ApplicationContext())
            {
                if (!db.AdsProfiles.Any(x => x.chat_id == chatId))
                {
                    if (lang == "eng")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "You don't have an ad yet, contact @nazar067 to purchase one.");
                    }
                    if (lang == "ukr")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас ще немає реклами, зверніться до @nazar067, щоб її придбати.");
                    }
                    if (lang == "rus")
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас еще нету рекламы, обратитесь к @nazar067, чтобы ее приобрести.");
                    }
                    return;
                }
            }

            if (lang == "eng")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "<b>Change the name (only you can see it)</b> \n/adsEditName ad's id new name, example /adsEditName 1 Rozetka\n\n" +
                            "<b>Change description</b> \n/adsEditDesc ad's id new description, example /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Enable/disable</b> \n/adsEditActive ad's id True/False, example /adsEditActive 1 True\n\n",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
            }
            if (lang == "ukr")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "<b>Змінити назву(тільки ви бачите її)</b> \n/adsEditName ad's id new name, наприклад /adsEditName 1 Rozetka\n\n" +
                            "<b>Змінити опис</b> \n/adsEditDesc ad's id new description, наприклад /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Активувати/деактивувати</b> \n/adsEditActive ad's id True/False, наприклад /adsEditActive 1 True\n\n",
                    replyToMessageId: update.Message.MessageId, 
                    parseMode: ParseMode.Html);
            }
            if (lang == "rus")
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "<b>Изменить название(только вы его видите)</b> \n/adsEditName ad's id new name, например /adsEditName 1 Rozetka\n\n" +
                            "<b>Изменить описание</b> \n/adsEditDesc ad's id new description, например /adsEditDesc 1 <u>Best shop in USA</u>\n\n" +
                            "<b>Активировать/деактивировать</b> \n/adsEditActive ad's id True/False, example /adsEditActive 1 True\n\n",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html);
            }
        }
        public async Task<string> ShowAds()
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                string listAds = null;
                var ads = await db.AdsProfiles
                    .Where(ad => ad.is_active == true && ad.is_activeAdmin == true) // Проверка условий
                    .OrderBy(ad => ad.Id)
                    .Take(3)
                    .Select(ad => ad.message) // Выбор только сообщения
                    .ToListAsync();
                foreach(var ad in ads)
                {
                    listAds += ad + "\n" + "\n";
                }
                return listAds;
            }
        }
        public async Task DeleteAds(long chatId)
        {
            string date = DateTime.Now.ToShortDateString();
            using (ApplicationContext db = new ApplicationContext())
            {
                AddToDataBase delete = new AddToDataBase();
                var adsWithTodayEndDate = await db.AdsProfiles
                    .Where(ad => ad.end_date == date)
                    .ToListAsync();

                foreach (var ad in adsWithTodayEndDate)
                {
                    await delete.DeleteAds(ad.Id, chatId);
                }
            }
            
        }
        private static string GetParseMode(Message message, string targetMessage)
        {
            var formattedText = targetMessage;
            var text = message.Text;
            var entityStack = new Stack<(string OpeningTag, string ClosingTag)>();

            if (message.Entities != null && message.Entities.Length > 0)
            {
                var offset = text.IndexOf(targetMessage);

                if (offset >= 0)
                {
                    var charFormat = new Dictionary<int, List<(string OpeningTag, string ClosingTag)>>();

                    foreach (var entity in message.Entities)
                    {
                        if (entity.Offset >= offset && entity.Offset + entity.Length <= offset + targetMessage.Length)
                        {
                            var relativeOffset = entity.Offset - offset;

                            string openingTag = "", closingTag = "";
                            switch (entity.Type)
                            {
                                case MessageEntityType.Bold:
                                    openingTag = "<b>";
                                    closingTag = "</b>";
                                    break;
                                case MessageEntityType.Italic:
                                    openingTag = "<i>";
                                    closingTag = "</i>";
                                    break;
                                case MessageEntityType.Code:
                                    openingTag = "<code>";
                                    closingTag = "</code>";
                                    break;
                                case MessageEntityType.Pre:
                                    openingTag = "<pre>";
                                    closingTag = "</pre>";
                                    break;
                                case MessageEntityType.TextLink:
                                    openingTag = $"<a href=\"{entity.Url}\">";
                                    closingTag = "</a>";
                                    break;
                                case MessageEntityType.TextMention:
                                    openingTag = "<a>";
                                    closingTag = "</a>";
                                    break;
                                case MessageEntityType.Spoiler:
                                    openingTag = "<tg-spoiler>";
                                    closingTag = "</tg-spoiler>";
                                    break;
                                case MessageEntityType.Underline:
                                    openingTag = "<u>";
                                    closingTag = "</u>";
                                    break;
                                case MessageEntityType.Strikethrough:
                                    openingTag = "<s>";
                                    closingTag = "</s>";
                                    break;
                            }

                            for (int i = relativeOffset; i < relativeOffset + entity.Length; i++)
                            {
                                if (!charFormat.ContainsKey(i))
                                {
                                    charFormat[i] = new List<(string OpeningTag, string ClosingTag)>();
                                }
                                charFormat[i].Add((openingTag, closingTag));
                            }
                        }
                    }

                    var sb = new StringBuilder();

                    for (int i = 0; i < targetMessage.Length; i++)
                    {
                        if (charFormat.ContainsKey(i))
                        {
                            foreach (var format in charFormat[i])
                            {
                                sb.Append(format.OpeningTag);
                                entityStack.Push(format);
                            }
                        }

                        sb.Append(targetMessage[i]);

                        if (charFormat.ContainsKey(i))
                        {
                            while (entityStack.Count > 0)
                            {
                                var format = entityStack.Pop();
                                sb.Append(format.ClosingTag);
                            }
                        }
                    }

                    formattedText = sb.ToString();
                }
            }

            return formattedText;
        }
    }
}

