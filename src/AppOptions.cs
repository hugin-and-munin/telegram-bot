namespace Bot;

public record AppOptions
{
    public const string Name = "AppOptions";
    public required string TelegramToken { get; init; }
    public required string ReportProviderUri { get; init; }
}
