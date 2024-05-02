using System.Collections.Concurrent;
using System.Text.Json;
using LegalEntityChecker;
using Microsoft.Extensions.Options;
using ReportProvider;
using Telegram.Bot.Types;
using static ReportProvider.ReportProvider;

namespace TelegramBot;

public class TelegramBotState
{
    private readonly ConcurrentDictionary<long, Mode> _modeCache = new();
    private readonly Recipient _notificationsChat;
    private readonly string _errorMessageTemplate = "Внимание, баклажаны 🍆🍆🍆\n\nПроверьте логи.\n\nCorrelation Id: {0}";
    private readonly string _modeGeneralSelectedMessage = "Вы выбрали режим: <b>ℹ️ Oбщая информация</b>";
    private readonly string _modeLegalEntitiesSelectedMessage = "Вы выбрали режим: <b>⚖️ Юридическая информация</b>";
    private readonly string _modeReviewsSelectedMessage = "Вы выбрали режим: <b>🗣️ Отзывы (в разработке)</b>";
    private readonly string _modeSalariesSelectedMessage = "Вы выбрали режим: <b>💰 Зарплаты (в разработке)</b>";
    private readonly string _generalInfoMode = "ℹ️ Oбщая информация";
    private readonly string _legalEntitiesInfoMode = "⚖️ Юридическая информация";
    private readonly string _reviewsMode = "🗣️ Отзывы (в разработке)";
    private readonly string _salariesMode = "💰 Зарплаты (в разработке)";
    private readonly string _startMessage;
    private readonly string _helpMessage;
    private readonly string _modesMessage;
    private readonly string _invalidTinMessage = "Неправильный ИНН.";
    private readonly string _bugMessage;
    private readonly string _footer;
    private readonly ILogger<TelegramBotState> _logger;
    private readonly ITelegramClientAdapter _telegram;
    private readonly ReportProviderClient _reportProvider;

    public TelegramBotState(
        IOptions<AppOptions> appOptions,
        ILogger<TelegramBotState> logger,
        ITelegramClientAdapter telegram,
        ReportProviderClient reportProvider
        )
    {

        _logger = logger;
        _telegram = telegram;
        _reportProvider = reportProvider;
        _notificationsChat = new Chat(appOptions.Value.NotificationsChatId, appOptions.Value.NotificationsTopicId);

        _footer = System.IO.File.OpenText("./Resources/Footer.html").ReadToEnd();
        _startMessage = System.IO.File.OpenText("./Resources/Start.html").ReadToEnd() + "\n\n" + _footer;
        _helpMessage = System.IO.File.OpenText("./Resources/Help.html").ReadToEnd() + "\n\n" + _footer;
        _modesMessage = System.IO.File.OpenText("./Resources/Mode.html").ReadToEnd();
        _bugMessage = System.IO.File.OpenText("./Resources/Bug.html").ReadToEnd() + "\n\n" + _footer;
    }

    private enum Mode
    {
        General = 0,
        LegalInfo = 1,
        Reviews = 2,
        Salaries = 3
    }

    public async Task Handle(Update update, CancellationToken ct = default)
    {
        try
        {
            await HandleImpl(update, ct);
        }
        catch (Exception e)
        {
            var guid = Guid.NewGuid();
            var message = string.Format(_errorMessageTemplate, guid);
            _logger.LogError(
                e,
                "Correlation Id: {guid}. Unhandled exception during processing: {update}.",
                guid,
                JsonSerializer.Serialize(update));
            await SendTextMessageAsync(_notificationsChat, message, ct);
        }
    }

    private async Task HandleImpl(Update update, CancellationToken ct = default)
    {
        var @event = await GetEvent(update, ct);

        switch (@event)
        {
            // Basic messages
            case SendGreetings e:
                await SendTextMessageAsync(e.Recipient, _startMessage, ct);
                break;
            case SendHelp e:
                await SendTextMessageAsync(e.Recipient, _helpMessage, ct);
                break;
            case SendModeSelection e:
                await SendCallbackMessageAsync(e.Recipient, _modesMessage, e.Data, e.ButtonsPerRow, ct);
                break;
            // Reports
            case SendGeneralReport e:
                await SendTextMessageAsync(e.Recipient, e.Report, ct);
                break;
            case SendLegalEntityReport e:
                await SendCallbackMessageAsync(e.Recipient, e.Report, e.Data, e.ButtonsPerRow, ct);
                break;
            case SendReviewsReport e:
                await SendTextMessageAsync(e.Recipient, e.Report, ct);
                break;
            case SendSalariesReport e:
                await SendTextMessageAsync(e.Recipient, e.Report, ct);
                break;
            // Modes
            case SendModeIsGeneral e:
                await SendTextMessageAsync(e.Recipient, _modeGeneralSelectedMessage, ct);
                break;
            case SendModeIsLegalEntityInfo e:
                await SendTextMessageAsync(e.Recipient, _modeLegalEntitiesSelectedMessage, ct);
                break;
            case SendModeIsReviews e:
                await SendTextMessageAsync(e.Recipient, _modeReviewsSelectedMessage, ct);
                break;
            case SendModeIsSalaries e:
                await SendTextMessageAsync(e.Recipient, _modeSalariesSelectedMessage, ct);
                break;
            // Errors
            case SendTinIsInvalid e:
                await SendTextMessageAsync(e.Recipient, _invalidTinMessage, ct);
                break;
            case SendNoContent e:
                _logger.LogError("Message without content received: {update}", JsonSerializer.Serialize(update));
                await SendTextMessageAsync(e.Recipient, _bugMessage, ct);
                break;
            // Misc
            case NoAction:
                // This is not a error, just ignore
                break;
        }
    }

    public async Task<Command> GetEvent(Update update, CancellationToken ct)
    {
        Message? message = update switch
        {
            { CallbackQuery.Message: not null } u => u.CallbackQuery.Message,
            { Message: not null } u => u.Message,
            _ => default
        };

        // Just ignore other types of updates, this is not a error
        if (message == default) return new NoAction();

        if (message.From == null) return new NoAction();

        var userId = message.From.Id;

        // Determine the source of the message
        Recipient? source = message switch
        {
            _ when message.Chat.Id == message.From.Id => new User(message.From.Id),
            _ => new Chat(message.Chat.Id, message.MessageThreadId, message.MessageId),
        };

        // Ignore if From == null, this is not a error
        if (source == default) return new NoAction();

        // Extract the content
        string? content = update switch
        {
            { CallbackQuery: not null } x => x.CallbackQuery.Data,
            { Message: not null } x => x.Message.Text,
            _ => default
        };

        if (content == default) return new SendNoContent(source);

        if (update.Message is not null)
        {
            // commands
            if (content.StartsWith("/start")) return new SendGreetings(source);
            if (content.StartsWith("/help")) return new SendHelp(source);
            if (content.StartsWith("/mode")) return GetModes(source, userId);
            if (content.StartsWith("/check")) return await GetReport(source, userId, content, ct);
        }

        if (update.CallbackQuery is not null)
        {
            // callbacks
            if (content.StartsWith("mode-")) return ChangeMode(source, content, userId);
            if (content.StartsWith("check-")) return await GetReport(source, userId, content, ct);
        }

        return new NoAction();
    }

    private Command ChangeMode(Recipient source, string content, long userId)
    {
        var span = content.AsSpan();

        // The user must be the one who sent the message
        var dash = span.LastIndexOf('-');
        if (userId != long.Parse(span[dash..])) return new NoAction();

        // Determine the mode
        var mode = span switch
        {
            _ when span.StartsWith("mode-general") => Mode.General,
            _ when span.StartsWith("mode-legalinfo") => Mode.LegalInfo,
            _ when span.StartsWith("mode-reviews") => Mode.Reviews,
            _ when span.StartsWith("mode-salaries") => Mode.Salaries,
            _ => throw new NotSupportedException($"Unsupported mode '{content}'")
        };

        // Update the cache
        _modeCache.AddOrUpdate(userId, x => mode, (_, _) => mode);

        return mode switch
        {
            Mode.General => new SendModeIsGeneral(source),
            Mode.LegalInfo => new SendModeIsLegalEntityInfo(source),
            Mode.Reviews => new SendModeIsReviews(source),
            Mode.Salaries => new SendModeIsSalaries(source),
            _ => throw new NotSupportedException($"Unsupported mode '{mode}'")
        };
    }

    private Command GetModes(Recipient source, long userId)
    {
        CallbackData[] modes =
        [
            new CallbackData(_generalInfoMode, $"mode-general-{userId}"),
            new CallbackData(_legalEntitiesInfoMode, $"mode-legalinfo-{userId}"),
            new CallbackData(_reviewsMode, $"mode-reviews-{userId}"),
            new CallbackData(_salariesMode, $"mode-salaries-{userId}"),
        ];
        return new SendModeSelection(source, modes, 1);
    }

    private async Task<Command> GetReport(Recipient source, long userId, string content, CancellationToken ct)
    {
        // If the user has not selected mode yet, send the message again
        if (!_modeCache.TryGetValue(userId, out var mode))
        {
            return GetModes(source, userId);
        }

        // Handle invalid TINs
        if (!TinParser.TryGetTin(content, out var tin))
        {
            return new SendTinIsInvalid(source);
        }

        var request = new ReportRequest() { Tin = tin };

        switch (mode)
        {
            case Mode.General:
                var generalInfo = await _reportProvider.GetGeneralInfoAsync(request, cancellationToken: ct);
                if (generalInfo == null) return new SendCompanyNotFound(source);
                var generalInfoReport = ReportConverter
                    .ToTelegramMessage(generalInfo)
                    .AppendLine()
                    .Append(_footer)
                    .ToString();
                return new SendGeneralReport(source, generalInfoReport);
            case Mode.LegalInfo:
                var legalEntityInfo = await _reportProvider.GetLegalEntityInfoAsync(request, cancellationToken: ct);
                if (legalEntityInfo == null) return new SendCompanyNotFound(source);
                var legalEntityInfoReport = ReportConverter
                    .ToTelegramMessage(legalEntityInfo)
                    .AppendLine()
                    .Append(_footer)
                    .ToString();
                var callbackData = legalEntityInfo.BasicInfo.Shareholders
                    // Only companies are supported right now
                    .Where(x => x.Type == EntityType.Company)
                    .Select(x => (x.Name, x.Tin))
                    // .Append((report.BasicInfo.Manager.Name, report.BasicInfo.Manager.Tin))
                    .Select(entity => new CallbackData(entity.Name, $"check-{userId}-{entity.Tin}"))
                    .ToArray();
                return new SendLegalEntityReport(source, legalEntityInfoReport, callbackData, 1);
            case Mode.Reviews:
                // TODO:
                // var reviewsInfo = await _reportProvider.GetReviewsInfoAsync(request, cancellationToken: ct);
                return new SendReviewsReport(source, "В разработке...");
            case Mode.Salaries:
                // TODO:
                // var salariesInfo = await _reportProvider.GetSalariesInfoAsync(request, cancellationToken: ct);
                return new SendSalariesReport(source, "В разработке...");
            default:
                throw new NotSupportedException();
        }
    }

    private Task SendTextMessageAsync(Recipient source, string text, CancellationToken ct)
    {
        return source switch
        {
            User user => _telegram.SendTextMessageAsync(user, text, ct),
            Chat chat => _telegram.SendTextMessageAsync(chat, text, ct),
            _ => Task.CompletedTask
        };
    }

    private Task SendCallbackMessageAsync(Recipient source, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct)
    {
        return source switch
        {
            User user => _telegram.SendCallbackMessageAsync(user, text, data, buttonsPerRow, ct),
            Chat chat => _telegram.SendCallbackMessageAsync(chat, text, data, buttonsPerRow, ct),
            _ => Task.CompletedTask
        };
    }
}

public abstract record Recipient;
public record Chat(long ChatId, int? TopicId, long? MessageId = null) : Recipient;
public record User(long UserId) : Recipient;

public record CallbackData(string Caption, string Data);

public abstract record Command;
public record NoAction : Command;
public record SendNoContent(Recipient Recipient) : Command;
public record SendTinIsInvalid(Recipient Recipient) : Command;
public record SendCompanyNotFound(Recipient Recipient) : Command;
public record SendGreetings(Recipient Recipient) : Command;
public record SendHelp(Recipient Recipient) : Command;
public record SendModeSelection(Recipient Recipient, CallbackData[] Data, int ButtonsPerRow) : Command;
public record SendGeneralReport(Recipient Recipient, string Report) : Command;
public record SendLegalEntityReport(Recipient Recipient, string Report, CallbackData[] Data, int ButtonsPerRow) : Command;
public record SendReviewsReport(Recipient Recipient, string Report) : Command;
public record SendSalariesReport(Recipient Recipient, string Report) : Command;
public record SendModeIsGeneral(Recipient Recipient) : Command;
public record SendModeIsLegalEntityInfo(Recipient Recipient) : Command;
public record SendModeIsReviews(Recipient Recipient) : Command;
public record SendModeIsSalaries(Recipient Recipient) : Command;
