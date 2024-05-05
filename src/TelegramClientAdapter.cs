using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot;

public interface ITelegramClientAdapter
{
    Task RemoveKeyboard(User user, int messageId, CancellationToken ct);
    Task RemoveKeyboard(Chat chat, int messageId, CancellationToken ct);
    Task SendTextMessageAsync(User user, string text, CancellationToken ct);
    Task SendTextMessageAsync(Chat chat, string text, CancellationToken ct);
    Task SendCallbackMessageAsync(Chat chat, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct);
    Task SendCallbackMessageAsync(User user, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct);
}

public class TelegramClientAdapter(TelegramBotClient _client) : ITelegramClientAdapter
{
    public Task RemoveKeyboard(User user, int messageId, CancellationToken ct)
    {
        return _client.EditMessageReplyMarkupAsync(
            chatId: user.UserId,
            messageId: messageId,
            cancellationToken: ct);
    }

    public Task RemoveKeyboard(Chat chat, int messageId, CancellationToken ct)
    {
        return _client.EditMessageReplyMarkupAsync(
            chatId: chat.ChatId,
            messageId: messageId,
            cancellationToken: ct);
    }

    public Task SendCallbackMessageAsync(
        Chat chat, 
        string text, 
        CallbackData[] data, 
        int buttonsPerRow, 
        CancellationToken ct)
    {
        var keyboard = data
            .Select(x => InlineKeyboardButton.WithCallbackData(x.Caption, x.Data))
            .Chunk(buttonsPerRow)
            .ToArray();

        return _client.SendTextMessageAsync(
            chatId: chat.ChatId,
            replyToMessageId: chat.MessageId,
            text: text,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(keyboard),
            cancellationToken: ct);
    }

    public Task SendCallbackMessageAsync(User user, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct)
    {
        var keyboard = data
            .Select(x => InlineKeyboardButton.WithCallbackData(x.Caption, x.Data))
            .Chunk(buttonsPerRow)
            .ToArray();

        return _client.SendTextMessageAsync(
            chatId: user.UserId,
            text: text,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(keyboard),
            cancellationToken: ct);
    }

    public Task SendTextMessageAsync(User user, string text, CancellationToken ct)
    {
        return _client.SendTextMessageAsync(
            chatId: user.UserId,
            text: text,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public Task SendTextMessageAsync(Chat chat, string text, CancellationToken ct)
    {
        return _client.SendTextMessageAsync(
            chatId: chat.ChatId,
            replyToMessageId: chat.MessageId,
            text: text,
            disableWebPagePreview: true,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}