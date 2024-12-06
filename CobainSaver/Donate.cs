using CobainSaver.DataBase;
using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CobainSaver
{
    internal class Donate
    {
        public async Task DonateMessage(string chatId, TelegramBotClient botClient, Update update, string amount)
        {
            int correctAmount = amount == "/donate" ? 50 : Convert.ToInt32(amount);

            Language language = new Language("rand", "rand");
            string lang = await language.GetCurrentLanguage(chatId);

            string title, description;
            if (lang == "rus")
            {
                title = "Сделайте CobainSaver лучше";
                description = "Поддержите нас, чтобы в будущем разработка была легче, и обновления выходили чаще";
            }
            else if (lang == "ukr")
            {
                title = "Зробіть CobainSaver краще";
                description = "Підтримайте нас, щоб у майбутньому розробка була і легшою, і оновлення виходили частіше";
            }
            else // Default to eng
            {
                title = "Make CobainSaver better";
                description = "Support us so that future development will be easier and updates will come out more frequently";
            }

            await botClient.SendInvoiceAsync(
                chatId: long.Parse(chatId),
                title: title,
                description: description,
                payload: "donate",
                currency: "XTR",
                prices: new[] { new Telegram.Bot.Types.Payments.LabeledPrice("Price", correctAmount) }
            );
        }

        public async Task HandlePreCheckoutQuery(TelegramBotClient botClient, Update update, long userId, int stars)
        {
            if (update.PreCheckoutQuery is { InvoicePayload: "donate", Currency: "XTR", TotalAmount: _ })
            {
                AddToDataBase addDB = new AddToDataBase();
                await botClient.AnswerPreCheckoutQueryAsync(update.PreCheckoutQuery.Id);
                await addDB.AddUserDonates(Convert.ToInt64(userId), stars, DateTime.Now.ToShortDateString());
            }
            else
            {
                await botClient.AnswerPreCheckoutQueryAsync(update.PreCheckoutQuery.Id, "Invalid order details.");
            }
        }

        public async Task HandleSuccessfulPayment(TelegramBotClient botClient, Update update)
        {
            if (update.Message.SuccessfulPayment.InvoicePayload == "donate")
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Thank you for your donation! Your support means a lot to us.");
            }
        }
    }
}
