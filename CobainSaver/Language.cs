﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using YoutubeExplode.Channels;

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
            string currentDirectory = Directory.GetCurrentDirectory();

            string userFolderName = "UserLogs";
            string userFolderPath = Path.Combine(currentDirectory, userFolderName);

            string folderName = chatId;
            string folderPath = Path.Combine(userFolderPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string lastFolderName = "serv";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                Directory.CreateDirectory(lastFolderPath);
            }

            string file = $"{Lang}.txt";
            string filePath = Path.Combine(lastFolderPath, file);

            string filePathCheckUkr = Path.Combine(lastFolderPath, "ukr.txt");
            string filePathCheckEng = Path.Combine(lastFolderPath, "eng.txt");
            string filePathCheckRus = Path.Combine(lastFolderPath, "rus.txt");
            if (System.IO.File.Exists(filePathCheckUkr))
            {
                System.IO.File.Move(filePathCheckUkr, filePath);
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: Message);
            }
            else if (System.IO.File.Exists(filePathCheckEng))
            {
                System.IO.File.Move(filePathCheckEng, filePath);
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: Message);
            }
            else if (System.IO.File.Exists(filePathCheckRus))
            {
                System.IO.File.Move(filePathCheckRus, filePath);
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: Message);
            }
            else if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, Lang);
            }
            else
            {
                System.IO.File.AppendAllText(filePath, Lang);
            }
        }
        public async Task StartLanguage(string chatId, TelegramBotClient botClient)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string userFolderName = "UserLogs";
            string userFolderPath = Path.Combine(currentDirectory, userFolderName);

            string folderName = chatId;
            string folderPath = Path.Combine(userFolderPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string lastFolderName = "serv";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                Directory.CreateDirectory(lastFolderPath);
            }

            if (Lang == "en")
            {
                Lang = "eng";
            }
            if (Lang == "uk")
            {
                Lang = "ukr";
            }
            if (Lang == "ru")
            {
                Lang = "rus";
            }

            string file = $"{Lang}.txt";
            string filePath = Path.Combine(lastFolderPath, file);

            string filePathCheckUkr = Path.Combine(lastFolderPath, "ukr.txt");
            string filePathCheckEng = Path.Combine(lastFolderPath, "eng.txt");
            string filePathCheckRus = Path.Combine(lastFolderPath, "rus.txt");
            if (System.IO.File.Exists(filePathCheckUkr))
            {
                System.IO.File.Move(filePathCheckUkr, filePath);
            }
            else if (System.IO.File.Exists(filePathCheckEng))
            {
                System.IO.File.Move(filePathCheckEng, filePath);
            }
            else if (System.IO.File.Exists(filePathCheckRus))
            {
                System.IO.File.Move(filePathCheckRus, filePath);
            }
            else if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, Lang);
            }
            else
            {
                System.IO.File.AppendAllText(filePath, Lang);
            }
        }
        public async Task<string> GetCurrentLanguage(string chatId)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string userFolderName = "UserLogs";
            string userFolderPath = Path.Combine(currentDirectory, userFolderName);

            string folderName = chatId;
            string folderPath = Path.Combine(userFolderPath, folderName);
            string lastFolderName = "serv";
            string lastFolderPath = Path.Combine(folderPath, lastFolderName);
            if (!Directory.Exists(lastFolderPath))
            {
                return "eng";
            }
            string filePathCheckUkr = Path.Combine(lastFolderPath, "ukr.txt");
            string filePathCheckEng = Path.Combine(lastFolderPath, "eng.txt");
            string filePathCheckRus = Path.Combine(lastFolderPath, "rus.txt");
            if (System.IO.File.Exists(filePathCheckUkr))
            {
                return "ukr";
            }
            else if (System.IO.File.Exists(filePathCheckEng))
            {
                return "eng";
            }
            else if (System.IO.File.Exists(filePathCheckRus))
            {
                return "rus";
            }
            return "eng";
        }
    }
}
