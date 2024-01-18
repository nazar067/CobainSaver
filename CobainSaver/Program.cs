using System.IO;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;

namespace CobainSaver
{
    class Program
    {
        //private static ITelegramBotClient _botClient;

        // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
        private static ReceiverOptions _receiverOptions;

        static async Task Main()
        {
            var botClient = new TelegramBotClient("Your API key");
            _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
            {
                AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
                {
                UpdateType.Message, // Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
            },
                // Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
                // True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();

            // UpdateHander - обработчик приходящих Update`ов
            // ErrorHandler - обработчик ошибок, связанных с Bot API
            botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // Запускаем бота

            var cobain = await botClient.GetMeAsync(); // Создаем переменную, в которую помещаем информацию о нашем боте.
            Console.WriteLine($"Cobain here");

            await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно
        }
        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try
            {
                var cobain = await botClient.GetMeAsync();
                // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            // эта переменная будет содержать в себе все связанное с сообщениями
                            var message = update.Message;
                            Downloader video = new Downloader();
                            // From - это от кого пришло сообщение (или любой другой Update)
                            var user = message.From;
                            var chat = message.Chat;
                            Logs logs = new Logs(message.Chat.Id, message.From.Id, message.From.Username, message.Text, null);
                            await logs.WriteUserLogs();
                            if (message.Text.Contains("https://www.youtube.com") || message.Text.Contains("https://youtu.be") || message.Text.Contains("https://youtube.com"))
                            {
                                await video.YoutubeDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                            }
                            else if (message.Text.Contains("https://vm.tiktok.com") || message.Text.Contains("https://www.tiktok.com"))
                            {
                                await video.TikTokDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                            }
                            else if(message.Text.Contains("https://www.reddit.com") || message.Text.Contains("https://redd.it/"))
                            {
                                await video.ReditDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                            }
                            else if (message.Text.Contains("https://x.com/") || message.Text.Contains("https://twitter.com/"))
                            {
                                await video.TwitterDownloader(chat.Id, update, cancellationToken, message.Text, (TelegramBotClient)botClient);
                            }
                            else if(message.Text.StartsWith("/logs") || message.Text.StartsWith($"/logs@{cobain.Username}"))
                            {
                                string dateLog = message.Text.Split(' ').Last();
                                await logs.SendUserLogs(dateLog, chat.Id.ToString(), update, cancellationToken, message.Text, (TelegramBotClient)botClient, cobain.Username);
                            }
                            else if (message.Text == "/start" || message.Text.StartsWith($"/start@{cobain.Username}"))
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "Hi, I'm CobainSaver, just send me video's link"
                                    );
                            }
                            else if(message.Text == "/help" || message.Text.StartsWith($"/help@{cobain.Username}"))
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: chat.Id,
                                    text: "/help - see all commands\n /logs - look at chat server logs",
                                    replyToMessageId: update.Message.MessageId
                                    );
                            }
                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                var message = update.Message;
                var user = message.From;
                var chat = message.Chat;
                Logs logs = new Logs(chat.Id, user.Id, user.Username, null, ex.ToString());
                await logs.WriteServerLogs();
            }
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}