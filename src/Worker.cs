using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot;

public class Worker(
    ILogger<Worker> _logger,
    IOptionsMonitor<AppOptions> _appOptions,
    CheckHandler _checkHandler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var client = new TelegramBotClient(_appOptions.CurrentValue.TelegramToken);

        _logger.LogInformation("Telegram Bot message handler started");

        await client.ReceiveAsync(HandleUpdate, HandleError, cancellationToken: ct);
    }

    private async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        // Ignore such updates
        if (update.Message == null) return;

        var (command, commandText, chatId) = update.Message.ParseTelegramCommand();

        if (command == TelegramCommands.Unknown)
        {
            await client.SendTextMessageAsync(chatId, $"Неизвестная команда", cancellationToken: ct);
            return;
        }

        Task task = command switch
        {
            TelegramCommands.Check => CheckHandler(client, chatId, commandText, ct),
            _ => Task.CompletedTask
        };

        await task;
    }

    private async Task CheckHandler(ITelegramBotClient client, long chatId, string commandText, CancellationToken ct)
    {
        if (!TelegramHelper.TryGetTin(commandText, out var tin)) return;

        var report = await _checkHandler.Handle(tin, ct);

        if (string.IsNullOrEmpty(report))
        {
            await client.SendTextMessageAsync(chatId, $"Не могу найти компанию с таким ИНН", cancellationToken: ct);
        }

        var buttons = new[]
        {
            InlineKeyboardButton.WithCallbackData("🗣️ Отзывы", $"2"),
            InlineKeyboardButton.WithCallbackData("💲 Зарплаты", $"3"),
            InlineKeyboardButton.WithCallbackData("⚖️ Юридическая информация", $"1"),
        };

        var replyMarkup = new InlineKeyboardMarkup(buttons.Chunk(2));

        await client.SendTextMessageAsync(
            chatId,
            report,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: ct);
    }

    private async Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception");
        await Task.CompletedTask;
        throw exception;
    }
}
