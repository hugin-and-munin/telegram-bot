using Grpc.Core;
using Grpc.Health.V1;
using Telegram.Bot;
using static Grpc.Health.V1.HealthCheckResponse.Types;

namespace TelegramBot;

public class HealthCheck(TelegramBotClient _telegramBot, ILogger<HealthCheck> _logger) : Health.HealthBase
{
    public override async Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        ServingStatus status;

        try
        {
            var me = await _telegramBot.GetMeAsync(context.CancellationToken);
            status = ServingStatus.Serving;
        }
        catch (Exception e)
        {
            status = ServingStatus.NotServing;
            _logger.LogError(e, "Healthcheck failed.");
        }

        return new HealthCheckResponse() { Status = status };
    }
}