namespace TelegramBot;

public static class TinParser
{
    private static readonly string _botName = "it_hugin_and_munin_bot";

    /// <summary>
    /// Tries to get the TIN (Taxpayer Identification Number) from the given command.
    /// </summary>
    /// <param name="command">The input command string</param>
    /// <param name="tin">The output TIN if found</param>
    /// <returns>True if the TIN is successfully extracted, otherwise false</returns>
    public static bool TryGetTin(string command, out long tin)
    {
        var span = command.AsSpan();
        tin = -1;

        // Determine whether it's a command or a callback
        span = span switch
        {
            _ when span.StartsWith("/check") => span["/check".Length..],
            _ when span.StartsWith("check-") => span["check-".Length..],
            _ => string.Empty,
        };

        if (span.Length == 0) return false;

        // Remove bot name if present
        if (span[0] == '@' && span[1..].StartsWith(_botName)) span = span[(_botName.Length + 1)..];
        if (span.Length == 0) return false;

        // Remove leading spaces if present
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
}