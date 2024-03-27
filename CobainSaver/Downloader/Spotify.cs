using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace CobainSaver.Downloader
{
    internal class Spotify
    {
        public async Task SpotifyDownloader(long chatId, Update update, CancellationToken cancellationToken, string messageText, TelegramBotClient botClient)
        {
            HttpClient spotifyClient = new HttpClient();

            string client_id = "";
            string client_secret = "";

            string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));

            var authOptions = new
            {
                url = "https://accounts.spotify.com/api/token",
                headers = new
                {
                    Authorization = "Basic " + base64Auth
                },
                form = new
                {
                    grant_type = "client_credentials"
                }
            };

            string accessToken = await GetAccessToken(authOptions, base64Auth);
            spotifyClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            Console.WriteLine("Access token: " + accessToken);
            var spotify = new SpotifyClient(accessToken);

            var track = await spotify.Tracks.Get("3pZcPE0ugJCI6zu5scIjp5");
            var response = await spotifyClient.GetAsync("https://api.spotify.com/v1/audio-analysis/2takcwOaAZWiXQijPHIx7B");
            await Console.Out.WriteLineAsync(response.StatusCode.ToString());
            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(responseString);
            Console.WriteLine(jsonObject);
            string currentDirectory = Directory.GetCurrentDirectory();

            string serverFolderName = "ServerLogs";
            string serverFolderPath = Path.Combine(currentDirectory, serverFolderName);

            string allServers = "jsonSpotify.json";
            string allFilePath = Path.Combine(serverFolderPath, allServers);
            if (!System.IO.File.Exists(allFilePath))
            {
                System.IO.File.WriteAllText(allFilePath, jsonObject.ToString());
            }
            else
            {
                System.IO.File.AppendAllText(allFilePath, jsonObject.ToString());
            }
        }
        static async Task<string> GetAccessToken(dynamic authOptions, string base64Auth)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64Auth);

                var response = await client.PostAsync(authOptions.url, content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                    return json.access_token;
                }
                else
                {
                    Console.WriteLine("Ошибка при запросе токена: " + response.StatusCode);
                    return null;
                }
            }
        }
    }
}
