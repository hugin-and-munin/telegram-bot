using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<long, Mode> _modeCache = new();

    private readonly string _startMessage = new StringBuilder()
        .AppendLine(@"<b>👋 Привет! Это <a href=""https://ru.wikipedia.org/wiki/Хугин_и_Мунин"">Hugin & Munin Bot</a>.</b>")
        .AppendLine()
        .AppendLine("Он многое знает об IT-компаниях:")
        .AppendLine()
        .AppendLine("💰 Зарплаты")
        .AppendLine("🗣️ Отзывы")
        .AppendLine("⚖️ Юридическую информацию")
        .AppendLine("📈 Финансовую информацию")
        .AppendLine()
        .AppendLine("Выбери режим через /mode, напиши <code>/check ИНН компании</code>, и я покажу что знаю.")
        .AppendLine()
        .AppendLine("Чтобы узнать больше, напиши /help.")
        .AppendFooter()
        .ToString();

    private readonly string _helpMessage = new StringBuilder()
        .AppendLine("<b>Hugin & Munin Bot.</b>")
        .AppendLine()
        .AppendLine("Доступные команды:")
        .AppendLine()
        .AppendLine("/mode - Выбрать режим.")
        .AppendLine("/check - проверить компанию по ИНН.")
        .AppendLine("/help - помощь по боту.")
        .AppendFooter()
        .ToString();

    private readonly string _selectModeMessage = new StringBuilder()
        .AppendLine("<b>Выберите режим</b>")
        .AppendLine()
        .AppendLine("Чтобы работать с ботом, выберите один из режимов:")
        .ToString();

    private readonly string _errorMessage = new StringBuilder()
        .AppendLine("<b>🪲 Что-то пошло не так</b>")
        .AppendLine()
        .AppendLine("Наша команда разработчиков уже исправляет проблему.")
        .AppendFooter()
        .ToString();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var client = new TelegramBotClient(_appOptions.CurrentValue.TelegramToken);

        var commands = new BotCommand[]
        {
            new () { Command = "start", Description = "Начать работу с ботом" },
            new () { Command = "mode", Description = "Выбрать режим" },
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

    private async Task HandleCallback(
        ITelegramBotClient client,
        CallbackQuery callbackQuery,
        CancellationToken ct)
    {
        await client.EditMessageReplyMarkupAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            cancellationToken: ct);

        HandleCallback(callbackQuery.From.Id, callbackQuery.Data!);
    }

    private void HandleCallback(long userId, string callbackData)
    {
        var span = callbackData.AsSpan();

        if (span.StartsWith("mode-general")) _modeCache.TryAdd(userId, Mode.General);
        else if (span.StartsWith("mode-legalinfo")) _modeCache.TryAdd(userId, Mode.LegalInfo);
        else if (span.StartsWith("mode-reviews")) _modeCache.TryAdd(userId, Mode.Reviews);
        else if (span.StartsWith("mode-salaries")) _modeCache.TryAdd(userId, Mode.Salaries);
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
            TelegramCommands.Mode => HandleSelectMode(client, chatId, ct),
            TelegramCommands.Help => HelpHandle(client, chatId, ct),
            TelegramCommands.Check => CheckHandler(client, message.From!.Id, chatId, commandText, ct),
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
        long userId,
        string commandText,
        CancellationToken ct)
    {
        // First of all, the user has to select mode
        if (!_modeCache.TryGetValue(userId, out var mode))
        {
            await HandleSelectMode(client, chatId, ct);
            return;
        }

        // Handle invalid TINs
        if (!TelegramHelper.TryGetTin(commandText, out var tin))
        {
            await client.SendTextMessageAsync(
                chatId,
                $"Неправильный ИНН",
                cancellationToken: ct);
            return;
        }

        var task = mode switch
        {
            Mode.General => HandleGeneral(client, chatId, tin, ct),
            Mode.LegalInfo => HandleLegalInfo(client, chatId, tin, ct),
            Mode.Reviews => Task.CompletedTask,
            _ => throw new NotSupportedException()
        };

        await task;
    }

    private async Task HandleSelectMode(
        ITelegramBotClient client,
        long chatId,
        CancellationToken ct)
    {
        var buttons = new[]
        {
            InlineKeyboardButton.WithCallbackData("ℹ️ Oбщая информация", $"mode-general"),
            InlineKeyboardButton.WithCallbackData("⚖️ Юридическая информация", $"mode-legalinfo"),
            InlineKeyboardButton.WithCallbackData("🗣️ Отзывы (в разработке)", $"mode-reviews"),
            InlineKeyboardButton.WithCallbackData("💰 Зарплаты (в разработке)", $"mode-salaries"),
        };

        var replyMarkup = new InlineKeyboardMarkup(buttons.Chunk(1));

        await client.SendTextMessageAsync(
            chatId,
            _selectModeMessage,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: ct);
    }

    private async Task HandleGeneral(
        ITelegramBotClient client,
        long chatId,
        long tin,
        CancellationToken ct)
    {
        StringBuilder? report;

        try
        {
            report = await _checkHandler.Handle(tin, ct);
        }
        catch (Exception e)
        {
            await HandleError(client, chatId, e, ct);
            return;
        }

        if (report == default)
        {
            await client.SendTextMessageAsync(
                chatId,
                $"Компания с таким ИНН не найдена",
                cancellationToken: ct);
            return;
        }

        await client.SendTextMessageAsync(
            chatId,
            report.AppendFooter().ToString(),
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleLegalInfo(
        ITelegramBotClient client,
        long chatId,
        long tin,
        CancellationToken ct)
    {
        var report = await _checkHandler.Handle(tin, ct);

        if (report == default)
        {
            await client.SendTextMessageAsync(
                chatId,
                $"Компания с таким ИНН не найдена",
                cancellationToken: ct);
            return;
        }

        await client.SendTextMessageAsync(
            chatId,
            report.AppendFooter().ToString(),
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
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

    private async Task HandleError(
        ITelegramBotClient client,
        long chatId,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception");

        await client.SendTextMessageAsync(
            chatId,
            _errorMessage,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await Task.CompletedTask;
        throw exception;
    }

    enum Mode
    {
        General = 0,
        LegalInfo = 1,
        Reviews = 2,
        Salaries = 3
    }
}
