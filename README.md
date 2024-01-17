# CobainSaver
Telegram bot on c# which can save video from youtube, tiktok, reddit, twitter 
Put your API key in Program.cs ``` var botClient = new TelegramBotClient("Your API key");``` and download ffmpeg and specify path in Downloader.cs ``` ProcessStartInfo startInfo = new ProcessStartInfo("Path to ffmpeg");```
You need .net 8.0 to run bot
