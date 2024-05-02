namespace TelegramBot;

public interface ITelegramClientAdapter
{
    Task SendTextMessageAsync(User user, string text, CancellationToken ct);

    Task SendTextMessageAsync(Chat chat, string text, CancellationToken ct);

    Task SendCallbackMessageAsync(Chat chat, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct);

    Task SendCallbackMessageAsync(User user, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct);
}

public class TelegramClientAdapter : ITelegramClientAdapter
{
    public Task SendCallbackMessageAsync(Chat chat, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SendCallbackMessageAsync(User user, string text, CallbackData[] data, int buttonsPerRow, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SendTextMessageAsync(User user, string text, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SendTextMessageAsync(Chat chat, string text, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}