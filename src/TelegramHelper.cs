using Telegram.Bot.Types;

namespace Bot;

public static class TelegramHelper
{
    public static (TelegramCommands Command, string Text, long ChatId) ParseTelegramCommand(this Message message)
    {
        if (string.IsNullOrEmpty(message.Text))
        {
            return (TelegramCommands.Unknown, string.Empty, message.Chat.Id);
        }

        var span = message.Text.AsSpan();

        return span switch
        {
            var s when s.StartsWith("/check") => (TelegramCommands.Check, message.Text, message.Chat.Id),
            _ => (TelegramCommands.Unknown, string.Empty, message.Chat.Id)
        };
    }

    /// <summary>
    /// Tries to get the TIN (Taxpayer Identification Number) from the given command.
    /// </summary>
    /// <param name="command">The input command string</param>
    /// <param name="tin">The output TIN if found</param>
    /// <returns>True if the TIN is successfully extracted, otherwise false</returns>
    public static bool TryGetTin(string command, out long tin)
    {
        tin = 0L;

        var tinSpan = command.AsSpan()["/check".Length..].Trim();
        if (tinSpan.Length != 10 || !long.TryParse(tinSpan, out tin)) return false;

        return true;
    }
}

public enum TelegramCommands
{
    Unknown = 0,
    Check = 1
};