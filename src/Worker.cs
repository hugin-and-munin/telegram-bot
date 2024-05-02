using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TelegramBot;

public class Worker(
    ILogger<Worker> _logger,
    IOptions<AppOptions> _appOptions, 
    TelegramBotState _telegramBotState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var client = new TelegramBotClient(_appOptions.Value.TelegramToken);

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
            _telegramBotState.HandleUpdate,
            _telegramBotState.HandleError,
            receiverOptions: new ReceiverOptions()
            {
                AllowedUpdates = []
            },
            cancellationToken: ct);
    }
}