using System.Text;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;

public class Worker(
    ILogger<Worker> _logger,
    IOptionsMonitor<AppOptions> _appOptions,
    CheckHandler _checkHandler) : BackgroundService
{
    private readonly string _startMessage = new StringBuilder()
        .AppendLine("<b>👋 Привет! Я - бот, который многое знает об IT компаниях.</b>")
        .AppendLine()
        .AppendLine("Напиши <code>/check ИНН компании</code>, и я покажу информацию о ней.")
        .AppendLine()
        .AppendLine("Чтобы узнать больше, напиши /help.")
        .AppendFooter()
        .ToString();

    private readonly string _helpMessage = new StringBuilder()
        .AppendLine("<b>Hugin & Munin Bot.</b>")
        .AppendLine()
        .AppendLine("Доступные команды:")
        .AppendLine()
        .AppendLine("/check - проверить компанию по ИНН.")
        .AppendLine("/help - помощь по боту.")
        .AppendFooter()
        .ToString();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var client = new TelegramBotClient(_appOptions.CurrentValue.TelegramToken);

        var commands = new BotCommand[]
        {
            new () { Command = "start", Description = "Начать работу с ботом" },
            new () { Command = "help", Description = "Помощь" },
            new () { Command = "check", Description = "Проверить компанию по ИНН" }
        };

        await client.SetMyCommandsAsync(commands, cancellationToken: ct);

        _logger.LogInformation("Telegram Bot message handler started");

        await client.ReceiveAsync(
            HandleUpdate,
            HandleError,
            receiverOptions: new ReceiverOptions()
            {
                AllowedUpdates = []
            },
            cancellationToken: ct);
    }

    private Task HandleUpdate(
        ITelegramBotClient client,
        Update update,
        CancellationToken ct) => update switch
        {
            { CallbackQuery: not null } x => HandleCallback(client, x.CallbackQuery, ct),
            { Message: not null } x => HandleMessage(client, x.Message, ct),
            _ => Task.CompletedTask
        };

    private Task HandleCallback(
        ITelegramBotClient client,
        CallbackQuery callbackQuery,
        CancellationToken ct)
    {
        client.EditMessageReplyMarkupAsync(
            callbackQuery.Message!.Chat.Id, 
            callbackQuery.Message.MessageId, 
            cancellationToken: ct);
            
        var span = callbackQuery.Data.AsSpan();
        var separator = span.IndexOf('-');
        var tin = long.Parse(span[(separator + 1)..]);
        var task = span[..separator] switch
        {
            "reviews" => Task.CompletedTask,
            "salaries" => Task.CompletedTask,
            "legal_entities" => HandleExtendedLegalEntities(client, tin, ct),
            _ => throw new NotSupportedException()
        };

        return task;
    }

    private Task HandleExtendedLegalEntities(
        ITelegramBotClient client,
        long tin,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private Task HandleMessage(
        ITelegramBotClient client,
        Message message,
        CancellationToken ct)
    {
        var (command, commandText, chatId) = message.ParseTelegramCommand();

        if (command == TelegramCommands.Unknown) return Task.CompletedTask;

        return command switch
        {
            TelegramCommands.Start => StartHandle(client, chatId, ct),
            TelegramCommands.Help => HelpHandle(client, chatId, ct),
            TelegramCommands.Check => CheckHandler(client, chatId, commandText, ct),
            _ => Task.CompletedTask
        };
    }

    private Task<Message> StartHandle(
        ITelegramBotClient client,
        long chatId,
        CancellationToken ct) =>
            client.SendTextMessageAsync(
                chatId,
                _startMessage,
                disableWebPagePreview: true,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

    private Task<Message> HelpHandle(
        ITelegramBotClient client,
        long chatId,
        CancellationToken ct) =>
            client.SendTextMessageAsync(
                chatId,
                _helpMessage,
                disableWebPagePreview: true,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

    private async Task CheckHandler(
        ITelegramBotClient client,
        long chatId,
        string commandText,
        CancellationToken ct)
    {
        if (!TelegramHelper.TryGetTin(commandText, out var tin))
        {
            await client.SendTextMessageAsync(
                chatId,
                $"Неправильный ИНН",
                cancellationToken: ct);
            return;
        }

        var report = await _checkHandler.Handle(tin, ct);

        if (report == default)
        {
            await client.SendTextMessageAsync(
                chatId,
                $"Компания с таким ИНН не найдена",
                cancellationToken: ct);
            return;
        }

        var buttons = new[]
        {
            InlineKeyboardButton.WithCallbackData("🗣️ Отзывы", $"reviews-{tin}"),
            InlineKeyboardButton.WithCallbackData("💲 Зарплаты", $"salaries-{tin}"),
            InlineKeyboardButton.WithCallbackData("⚖️ Юридическая информация", $"legal_entites-{tin}"),
        };

        var replyMarkup = new InlineKeyboardMarkup(buttons.Chunk(2));

        await client.SendTextMessageAsync(
            chatId,
            report.AppendFooter().ToString(),
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: ct);
    }

    private async Task HandleError(
        ITelegramBotClient client,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception");
        await Task.CompletedTask;
        throw exception;
    }
}
