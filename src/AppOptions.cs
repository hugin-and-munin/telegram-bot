namespace TelegramBot;

public record AppOptions
{
    public const string Name = "AppOptions";
    public required long NotificationsChatId { get; init; }
    public required int NotificationsTopicId { get; init; }
    public required string TelegramToken { get; init; }
    public required string ReportProviderUri { get; init; }
}
