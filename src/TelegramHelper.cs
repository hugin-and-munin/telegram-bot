using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Telegram.Bot.Types;

namespace TelegramBot;

public static class TelegramHelper
{
    private static readonly string _botName = "it_hugin_and_munin_bot";
    public static (TelegramCommands Command, string Text, long ChatId) ParseTelegramCommand(this Message message)
    {
        if (string.IsNullOrEmpty(message.Text))
        {
            return (TelegramCommands.Unknown, string.Empty, message.Chat.Id);
        }

        var span = message.Text.AsSpan();

        return span switch
        {
            var s when s.StartsWith("/start") => (TelegramCommands.Start, message.Text, message.Chat.Id),
            var s when s.StartsWith("/help") => (TelegramCommands.Help, message.Text, message.Chat.Id),
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
        var span = command.AsSpan();

        span = span["/check".Length..];

        // Remove bot name
        if (span[0] == '@') span = span[(_botName.Length + 1)..];
        // Remove leading spaces
        if (span[0] == ' ') span = span[1..];

        span = span.Trim();

        // ИНН российского юридического лица - последовательность из 10 цифр
        if (!long.TryParse(span, out tin) ||
            tin < 1_000_000_000 ||
            tin > 9_999_999_999)
        {
            tin = -1;
            return false;
        }

        return true;
    }

    public static StringBuilder AppendFooter(this StringBuilder sb)
    {
        sb.AppendLine();
        sb.Append("<a href=\"https://github.com/hugin-and-munin\">GitHub</a>");
        sb.Append(" | ");
        sb.Append("<a href=\"https://t.me/it_hugin_and_munin\">Telegram</a>");
        return sb;
    }
}

public enum TelegramCommands
{
    Unknown = 0,
    Start = 1,
    Help = 2,
    Check = 3,
};