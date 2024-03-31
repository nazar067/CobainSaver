using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using VideoLibrary;
using System.Text.RegularExpressions;
using CobainSaver.DataBase;

namespace CobainSaver.Downloader
{
    internal class Twitter
    {
        private static readonly HttpClient client = new HttpClient();
        public async Task TwitterDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            AddToDataBase addDB = new AddToDataBase();

            string jsonString = System.IO.File.ReadAllText("source.json");
            JObject jsonObjectAPI = JObject.Parse(jsonString);
            string normallMsg = await DeleteNotUrl(messageText);
            string mediaId = null;
            if (normallMsg.Contains("https://x.com/"))
            {
                mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                mediaId = mediaId.Split('?').First();
            }
            else if (normallMsg.Contains("https://twitter.com/"))
            {
                mediaId = normallMsg.Remove(0, normallMsg.IndexOf("status/", StringComparison.InvariantCulture));
                if (mediaId.Contains("photo/"))
                {
                    mediaId = normallMsg.Remove(normallMsg.LastIndexOf("/"));
                }
            }
            var response = await client.GetAsync(jsonObjectAPI["XAPI"][0] + mediaId);
            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(responseString);
            string caption = jsonObject["text"].ToString();
            List<IAlbumInputMedia> mediaAlbum = new List<IAlbumInputMedia>();
            foreach (var album in jsonObject["mediaURLs"])
            {
                if (album.ToString().Contains("/media/"))
                {
                    mediaAlbum.Add(
                         new InputMediaPhoto(InputFile.FromUri(album.ToString()))
                         {
                             Caption = caption,
                             ParseMode = ParseMode.Markdown
                         }
                        );
                }
                else if (album.ToString().Contains("video."))
                {
                    if (!jsonObject["mediaURLs"].ToString().Contains("/media/"))
                    {
                        mediaAlbum.Add(
                             new InputMediaVideo(InputFile.FromUri(album.ToString()))
                             {
                                 Caption = caption,
                                 ParseMode = ParseMode.Markdown
                             }
                            );
                    }
                    else
                    {
                        mediaAlbum.Add(
                             new InputMediaVideo(InputFile.FromUri(album.ToString()))
                            );
                    }

                }
            }
            int rowSize = 10;
            List<List<IAlbumInputMedia>> result = ConvertTo2D(mediaAlbum, rowSize);
            foreach (var item in result)
            {
                await botClient.SendMediaGroupAsync(
                    chatId: chatId,
                    media: item,
                    replyToMessageId: update.Message.MessageId);
            }
            await addDB.AddBotCommands(chatId, "twitter", DateTime.Now.ToShortDateString());
        }
        static List<List<T>> ConvertTo2D<T>(List<T> arr, int rowSize)
        {
            List<List<T>> result = new List<List<T>>();
            for (int i = 0; i < arr.Count; i += rowSize)
            {
                result.Add(arr.GetRange(i, Math.Min(rowSize, arr.Count - i)));
            }
            return result;
        }
        public async Task<string> DeleteNotUrl(string message)
        {
            // Регулярное выражение для URL-адресов
            Regex regexUrl = new Regex(@"\bhttps://\S+\b");

            // Регулярное выражение для коротких ссылок YouTube
            Regex regexShortUrl = new Regex(@"youtu.be/\w+");

            // Поиск URL-адреса
            Match matchUrl = regexUrl.Match(message);

            // Поиск короткой ссылки YouTube
            Match matchShortUrl = regexShortUrl.Match(message);

            // Если найден URL-адрес, возвращаем его
            if (matchUrl.Success)
            {
                return matchUrl.Value;
            }

            // Если найдена короткая ссылка YouTube, добавляем "https://" и возвращаем
            if (matchShortUrl.Success)
            {
                return "https://" + matchShortUrl.Value;
            }

            // Если не найдено ни одного совпадения, возвращаем пустую строку
            return string.Empty;
        }
    }
}
