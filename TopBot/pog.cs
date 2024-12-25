//using System;
//using System.IO;
//using System.Text.Json;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Telegram.Bot;
//using Telegram.Bot.Exceptions;
//using Telegram.Bot.Polling;
//using Telegram.Bot.Types;
//using Telegram.Bot.Types.Enums;
//using Telegram.Bot.Types.ReplyMarkups;
//using File = System.IO.File;

//class Program
//{
//    private static TelegramBotClient botClient;
//    private static bool isAdminMode = false;
//    private static readonly string complaintsFile = "report.json";
//    private static readonly string questionsFile = "questions.json";

//    static async Task Main(string[] args)
//    {
//        botClient = new TelegramBotClient("7682210799:AAH_E_UPVJyTOcN02XHHjwKitsDxA9w0f2M");

//        var cts = new CancellationTokenSource();
//        var cancellationToken = cts.Token;
//        var receiverOptions = new ReceiverOptions
//        {
//            AllowedUpdates = Array.Empty<UpdateType>()
//        };

//        botClient.StartReceiving(
//            HandleUpdateAsync,
//            HandlePollingErrorAsync,
//            receiverOptions,
//            cancellationToken
//        );

//        var botMe = await botClient.GetMeAsync();
//        Console.WriteLine($"Бот {botMe.Username} запущен");

//        Console.ReadLine();
//        cts.Cancel();
//    }

//    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//    {
//        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
//        {
//            await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
//            return;
//        }

//        if (update.Type != UpdateType.Message || update.Message?.Text == null)
//            return;

//        var chatId = update.Message.Chat.Id;
//        var messageText = update.Message.Text;

//        if (messageText == "/start")
//        {
//            isAdminMode = false;
//            await ShowMainMenuAsync(chatId, cancellationToken);
//        }
//        else if (messageText == "admin")
//        {
//            isAdminMode = !isAdminMode;
//            await ShowMainMenuAsync(chatId, cancellationToken);
//        }
//        else if (update.Message.ReplyToMessage?.Text.StartsWith("Вы выбрали 'Жалоба'") == true)
//        {
//            SaveEntry(chatId, messageText, complaintsFile, "жалоба");
//            await botClient.SendTextMessageAsync(chatId, "Ваша жалоба сохранена. Спасибо!", cancellationToken: cancellationToken);
//        }
//        else if (update.Message.ReplyToMessage?.Text.StartsWith("Вы выбрали 'Вопрос'") == true)
//        {
//            SaveEntry(chatId, messageText, questionsFile, "вопрос");
//            await botClient.SendTextMessageAsync(chatId, "Ваш вопрос сохранен. Спасибо!", cancellationToken: cancellationToken);
//        }
//        else
//        {
//            await botClient.SendTextMessageAsync(chatId, "Я пока не понимаю эту команду.", cancellationToken: cancellationToken);
//        }

//    }

//    private static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
//    {
//        var chatId = callbackQuery.Message.Chat.Id;
//        var messageId = callbackQuery.Message.MessageId;

//        switch (callbackQuery.Data)
//        {
//            case "complaints_menu":
//                await ShowSubmenuAsync(chatId, messageId, "Меню жалоб:", "complaint", "main_menu", cancellationToken);
//                break;

//            case "questions_menu":
//                await ShowSubmenuAsync(chatId, messageId, "Меню вопросов:", "question", "main_menu", cancellationToken);
//                break;

//            case "faq_menu":
//                await botClient.EditMessageTextAsync(chatId, messageId, "Меню ЧаВо:",
//                    replyMarkup: new InlineKeyboardMarkup(new[]
//                    {
//                        InlineKeyboardButton.WithUrl("Перейти на сайт", "https://youtube.com/"),
//                        InlineKeyboardButton.WithCallbackData("Назад", "main_menu")
//                    }),
//                    cancellationToken: cancellationToken);
//                break;

//            case "main_menu":
//                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
//                await ShowMainMenuAsync(chatId, cancellationToken);
//                break;

//            case "complaint":
//                await botClient.SendTextMessageAsync(chatId, "Вы выбрали 'Жалоба'. Опишите вашу проблему.", cancellationToken: cancellationToken);
//                break;

//            case "question":
//                await botClient.SendTextMessageAsync(chatId, "Вы выбрали 'Вопрос'. Задайте ваш вопрос.", cancellationToken: cancellationToken);
//                break;

//            case "view_complaints":
//                await ShowEntriesAsync(chatId, complaintsFile, "жалоба", cancellationToken);
//                break;

//            case "view_questions":
//                await ShowEntriesAsync(chatId, questionsFile, "вопрос", cancellationToken);
//                break;

//            default:
//                if (callbackQuery.Data.StartsWith("reply_complaint_"))
//                {
//                    var id = callbackQuery.Data.Replace("reply_complaint_", "");
//                    await botClient.SendTextMessageAsync(chatId, $"Ответьте на жалобу ID: {id}", cancellationToken: cancellationToken);
//                }
//                else if (callbackQuery.Data.StartsWith("reply_question_"))
//                {
//                    var id = callbackQuery.Data.Replace("reply_question_", "");
//                    await botClient.SendTextMessageAsync(chatId, $"Ответьте на вопрос ID: {id}", cancellationToken: cancellationToken);
//                }
//                else if (callbackQuery.Data.StartsWith("reply_complaint_"))
//                {
//                    var id = callbackQuery.Data.Replace("reply_complaint_", "");
//                    await botClient.SendTextMessageAsync(chatId, $"Ответьте на жалобу ID: {id}", cancellationToken: cancellationToken);
//                }
//                else if (callbackQuery.Data.StartsWith("reply_question_"))
//                {
//                    var id = callbackQuery.Data.Replace("reply_question_", "");
//                    await botClient.SendTextMessageAsync(chatId, $"Ответьте на вопрос ID: {id}", cancellationToken: cancellationToken);
//                }
//                break;
//        }

//        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
//    }

//    private static async Task ShowMainMenuAsync(long chatId, CancellationToken cancellationToken)
//    {
//        var buttons = new List<InlineKeyboardButton[]> {
//            new[]
//            {
//                InlineKeyboardButton.WithCallbackData("Жалобы", "complaints_menu"),
//                InlineKeyboardButton.WithCallbackData("Вопрос", "questions_menu"),
//                InlineKeyboardButton.WithCallbackData("ЧаВо", "faq_menu")
//            }
//        };

//        if (isAdminMode)
//        {
//            buttons.Add(new[]
//            {
//                InlineKeyboardButton.WithCallbackData("Просмотр жалоб", "view_complaints"),
//                InlineKeyboardButton.WithCallbackData("Просмотр вопросов", "view_questions")
//            });
//        }

//        await botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Выберите действие:", replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
//    }

//    private static async Task ShowSubmenuAsync(long chatId, int messageId, string text, string action, string backAction, CancellationToken cancellationToken)
//    {
//        await botClient.EditMessageTextAsync(chatId, messageId, text,
//            replyMarkup: new InlineKeyboardMarkup(new[]
//            {
//                InlineKeyboardButton.WithCallbackData(action == "complaint" ? "Жалоба" : "Вопрос", action),
//                InlineKeyboardButton.WithCallbackData("Назад", backAction)
//            }),
//            cancellationToken: cancellationToken);
//    }

//    private static async Task ShowEntriesAsync(long chatId, string fileName, string entryType, CancellationToken cancellationToken)
//    {
//        if (!File.Exists(fileName))
//        {
//            await botClient.SendTextMessageAsync(chatId, $"Нет доступных {entryType}.", cancellationToken: cancellationToken);
//            return;
//        }

//        var data = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName));
//        if (data == null || data.Count == 0)
//        {
//            await botClient.SendTextMessageAsync(chatId, $"Нет доступных {entryType}.", cancellationToken: cancellationToken);
//            return;
//        }

//        var buttons = data.Select(e => InlineKeyboardButton.WithCallbackData($"{entryType} ID: {e.Id}", $"reply_{entryType}_{e.Id}")).ToArray();
//        await botClient.SendTextMessageAsync(chatId, $"Доступные {entryType}:", replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
//    }

//    private static void SaveEntry(long userId, string content, string fileName, string type)
//    {
//        var entry = new Entry
//        {
//            Id = Guid.NewGuid().ToString(),
//            UserId = userId,
//            Content = content
//        };

//        var list = new List<Entry>();
//        if (File.Exists(fileName))
//        {
//            var existingData = File.ReadAllText(fileName);
//            list = JsonSerializer.Deserialize<List<Entry>>(existingData) ?? new List<Entry>();
//        }

//        list.Add(entry);

//        var jsonData = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
//        File.WriteAllText(fileName, jsonData);

//    }

//    private class Entry
//    {
//        public string Id { get; set; }
//        public long UserId { get; set; }
//        public string Content { get; set; }
//    }

//    private static async Task HandleAdminReplyAsync(long chatId, string entryId, string entryType, string fileName, string replyText, CancellationToken cancellationToken)
//    {
//        if (!File.Exists(fileName))
//        {
//            await botClient.SendTextMessageAsync(chatId, $"Нет доступных {entryType} для ответа.", cancellationToken: cancellationToken);
//            return;
//        }

//        var data = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName));
//        var entry = data?.FirstOrDefault(e => e.Id == entryId);

//        if (entry == null)
//        {
//            await botClient.SendTextMessageAsync(chatId, $"{entryType} с ID: {entryId} не найден(а).", cancellationToken: cancellationToken);
//            return;
//        }

//        // Отправка ответа пользователю
//        await botClient.SendTextMessageAsync(entry.UserId, $"Ответ на ваш(у) {entryType}: {replyText}", cancellationToken: cancellationToken);

//        await botClient.SendTextMessageAsync(chatId, $"{entryType} с ID: {entryId} обработан(а).", cancellationToken: cancellationToken);
//    }
//    private static async Task HandleAdminReplyAsync(long chatId, string entryId, string fileName, string replyText, CancellationToken cancellationToken)
//    {
//        if (!File.Exists(fileName))
//        {
//            await botClient.SendTextMessageAsync(chatId, "Файл с данными не найден.", cancellationToken: cancellationToken);
//            return;
//        }

//        var data = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName));
//        var entry = data?.FirstOrDefault(e => e.Id == entryId);

//        if (entry == null)
//        {
//            await botClient.SendTextMessageAsync(chatId, "Элемент с указанным ID не найден.", cancellationToken: cancellationToken);
//            return;
//        }

//        await botClient.SendTextMessageAsync(entry.UserId, $"Ответ на вашу {Path.GetFileNameWithoutExtension(fileName)}: {replyText}", cancellationToken: cancellationToken);

//        await botClient.SendTextMessageAsync(chatId, "Ответ отправлен.", cancellationToken: cancellationToken);
//    }

//    static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
//    {
//        var ErrorMessage = exception switch
//        {
//            ApiRequestException apiRequestException
//                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
//            _ => exception.ToString()
//        };

//        Console.WriteLine(ErrorMessage);
//        return Task.CompletedTask;
//    }
//}