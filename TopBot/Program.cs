using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
[Obsolete]
class Program
{
    private static TelegramBotClient botClient;
    private static bool isAdminMode = false;
    private static readonly string complaintsFile = "complaints.json";
    private static readonly string questionsFile = "questions.json";
    private static List<string> adminSequence = new List<string>();
    private static CallbackQuery? tempReply;

    static async Task Main(string[] args)
    {
        botClient = new TelegramBotClient("7682210799:AAH_E_UPVJyTOcN02XHHjwKitsDxA9w0f2M");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        var botMe = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {botMe.Username} запущен");

        Console.ReadLine();
        cts.Cancel();
    }
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            tempReply = update.CallbackQuery;
            await HandleCallbackQueryAsync(botClient, update, update.CallbackQuery, cancellationToken);
            return;
        }
        else if (update.Message.ReplyToMessage?.Text.StartsWith("Введите ответ для ") == true && tempReply != null)
        {
            await HandleCallbackQueryAsync(botClient, update, tempReply, cancellationToken);
            return;
        }

        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;

        if (messageText == "/start")
        {
            isAdminMode = false;
            adminSequence.Clear();
            await ShowMainMenuAsync(chatId, cancellationToken);
        }
        else if (messageText == "admin")
        {
            adminSequence.Add("admin");

            if (adminSequence.Count == 2)
            {
                isAdminMode = true;
                adminSequence.Clear();
                await ShowMainMenuAsync(chatId, cancellationToken);
            }
        }
        else if (update.Message.ReplyToMessage?.Text.StartsWith("Вы выбрали 'Жалоба'") == true)
        {
            SaveEntry(chatId, messageText, complaintsFile, "жалоба");
            await botClient.SendTextMessageAsync(chatId, "Ваша жалоба сохранена. Спасибо!", cancellationToken: cancellationToken);
        }
        else if (update.Message.ReplyToMessage?.Text.StartsWith("Вы выбрали 'Вопрос'") == true)
        {
            SaveEntry(chatId, messageText, questionsFile, "вопрос");
            await botClient.SendTextMessageAsync(chatId, "Ваш вопрос сохранен. Спасибо!", cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Я пока не понимаю эту команду.", cancellationToken: cancellationToken);
        }
    }
    private static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        switch (callbackQuery.Data)
        {
            case "complaints_menu":
                await ShowSubmenuAsync(chatId, messageId, "Меню жалоб:", "complaint", "main_menu", cancellationToken);
                break;

            case "questions_menu":
                await ShowSubmenuAsync(chatId, messageId, "Меню вопросов:", "question", "main_menu", cancellationToken);
                break;

            case "faq_menu":
                await botClient.EditMessageTextAsync(chatId, messageId, "Меню ЧаВо:",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithUrl("Перейти на сайт", "https://youtube.com/"),
                        InlineKeyboardButton.WithCallbackData("Назад", "main_menu")
                    }),
                    cancellationToken: cancellationToken);
                break;

            case "main_menu":
                await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
                await ShowMainMenuAsync(chatId, cancellationToken);
                break;

            case "complaint":
                await botClient.SendTextMessageAsync(chatId, "Вы выбрали 'Жалоба'. Опишите вашу проблему.", cancellationToken: cancellationToken);
                break;

            case "question":
                await botClient.SendTextMessageAsync(chatId, "Вы выбрали 'Вопрос'. Задайте ваш вопрос.", cancellationToken: cancellationToken);
                break;

            case "view_complaints":
                await ShowEntriesAsync(chatId, complaintsFile, "жалоба", cancellationToken);
                break;

            case "view_questions":
                await ShowEntriesAsync(chatId, questionsFile, "вопрос", cancellationToken);
                break;

            default:
                if (callbackQuery.Data.StartsWith("reply_жалоба_"))
                {
                    var id = callbackQuery.Data.Replace("reply_жалоба_", "");
                    await AdminReply(update, chatId, id, complaintsFile, "жалоба", cancellationToken);
                }
                else if (callbackQuery.Data.StartsWith("reply_вопрос_"))
                {
                    var id = callbackQuery.Data.Replace("reply_вопрос_", "");
                    await AdminReply(update, chatId, id, questionsFile, "вопрос", cancellationToken);
                }
                break;
        }

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    [Obsolete]
    private static async Task AdminReply(Update update, long chatId, string id, string fileName, string entryType, CancellationToken cancellationToken)
    {
        if (!File.Exists(fileName))
        {
            await botClient.SendTextMessageAsync(chatId, $"Файл {entryType} отсутствует.", cancellationToken: cancellationToken);
            return;
        }

        var data = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName));
        var entry = data?.FirstOrDefault(e => e.Id == id);
        var textData = entry?.Description.ToString();
        if (entry == null)
        {
            await botClient.SendTextMessageAsync(chatId, $"{entryType} с ID {id} не найден.", cancellationToken: cancellationToken);
            return;
        }
            
        if (update.Type == UpdateType.Message && update.Message != null)
        {
            if (update.Message.ReplyToMessage?.Text.StartsWith($"Введите ответ для {entryType}") == true)
            {
                var replyText = update.Message.Text;
                await botClient.SendTextMessageAsync(entry.UserId, $"Администратор ответил на вашу {entryType}: {replyText}", cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(chatId, $"{entryType} с ID {id} успешно обработан.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            var replyMessage = await botClient.SendTextMessageAsync(chatId, $"Введите ответ для {entryType} ID {id} сообщение | {textData}:", cancellationToken: cancellationToken);
        }
    }
    private static async Task ShowMainMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var buttons = new List<InlineKeyboardButton[]> {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Жалобы", "complaints_menu"),
                InlineKeyboardButton.WithCallbackData("Вопрос", "questions_menu"),
                InlineKeyboardButton.WithCallbackData("ЧаВо", "faq_menu")
            }
        };

        if (isAdminMode)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Просмотр жалоб", "view_complaints"),
                InlineKeyboardButton.WithCallbackData("Просмотр вопросов", "view_questions")
            });
        }

        await botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Выберите действие:", replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
    }

    private static async Task ShowSubmenuAsync(long chatId, int messageId, string text, string action, string backAction, CancellationToken cancellationToken)
    {
        await botClient.EditMessageTextAsync(chatId, messageId, text,
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData(action == "complaint" ? "Жалоба" : "Вопрос", action),
                InlineKeyboardButton.WithCallbackData("Назад", backAction)
            }),
            cancellationToken: cancellationToken);
    }

    private static async Task ShowEntriesAsync(long chatId, string fileName, string entryType, CancellationToken cancellationToken)
    {
        if (!File.Exists(fileName))
        {
            await botClient.SendTextMessageAsync(chatId, $"Нет доступных {entryType}.", cancellationToken: cancellationToken);
            return;
        }

        var data = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName));
        if (data == null || data.Count == 0)
        {
            await botClient.SendTextMessageAsync(chatId, $"Нет доступных {entryType}.", cancellationToken: cancellationToken);
            return;
        }

        var buttons = data.Select(e => InlineKeyboardButton.WithCallbackData($"{entryType} ID: {e.Id}", $"reply_{entryType}_{e.Id}")).ToArray();//

        await botClient.SendTextMessageAsync(chatId, $"Список {entryType}:",
            replyMarkup: new InlineKeyboardMarkup(buttons.Append(InlineKeyboardButton.WithCallbackData("Назад", "main_menu"))),
            cancellationToken: cancellationToken);
    }

    private static async void SaveEntry(long chatId, string description, string fileName, string entryType)
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid().ToString(),
            UserId = chatId,
            Description = description
        };

        List<Entry> entries;
        if (File.Exists(fileName))
        {
            entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(fileName)) ?? new List<Entry>();
        }
        else
        {
            entries = new List<Entry>();
        }

        entries.Add(entry);
        File.WriteAllText(fileName, JsonSerializer.Serialize(entries));
    }

    private class Entry
    {
        public string Id { get; set; }
        public long UserId { get; set; }
        public string Description { get; set; }
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
