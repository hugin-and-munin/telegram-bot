using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TelegramBot;

[ExcludeFromCodeCoverage]
public class Worker(
    ILogger<Worker> _logger,
    TelegramBotClient _client, 
    TelegramBotState _telegramBotState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var commands = new BotCommand[]
        {
            new () { Command = "start", Description = "Начать работу с ботом" },
            new () { Command = "mode", Description = "Выбрать режим" },
            new () { Command = "help", Description = "Помощь" },
            new () { Command = "check", Description = "Проверить компанию по ИНН" }
        };

        await _client.SetMyCommandsAsync(commands, cancellationToken: ct);

        _logger.LogInformation("Telegram Bot message handler started");

        await _client.ReceiveAsync(
            async (_, update, ct) => await _telegramBotState.HandleUpdate(update, ct),
            async (_, ex, ct) => await _telegramBotState.HandleError(ex, "ReceiveAsync threw exception.", ct),
            receiverOptions: new ReceiverOptions() { AllowedUpdates = [] },
            cancellationToken: ct);
    }
}