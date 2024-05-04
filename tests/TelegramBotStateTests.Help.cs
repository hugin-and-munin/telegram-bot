using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using static ReportProvider.ReportProvider;

namespace TelegramBot.Tests;

public partial class TelegramBotStateTests
{
    [DataTestMethod]
    [DynamicData(nameof(HelpTestData), DynamicDataSourceType.Method)]
    public async Task HelpReturnsCorrectEvent(Update update, Command expected)
    {
        // Arrange
        var options = Options.Create(new AppOptions() 
        {
            TelegramToken = "token",
            ReportProviderUri = "report-provider-url",
            NotificationsChatId = 111,
            NotificationsTopicId = 222,
        });
        var logger = Mock.Of<ILogger<TelegramBotState>>();
        var telegramClient = new Mock<ITelegramClientAdapter>();
        var reportProviderClient = new Mock<ReportProviderClient>();
        var botState = new TelegramBotState(options, logger, telegramClient.Object, reportProviderClient.Object);

        // Act
        Command actual = await botState.GetEvent(update, CancellationToken.None);

        // Assert
        actual.Should().BeOfType(expected.GetType());
        actual.Should().BeEquivalentTo(expected, o => o.RespectingRuntimeTypes());
    }

    public static IEnumerable<object[]> HelpTestData() 
    {
        yield return 
        [
            new Update()
            {
                Message = new Message
                {
                    Text = "/help",
                    From = new() { Id = 333 },
                    Chat = new() { Id = 333 }
                }
            },
            new SendHelp(new User(333))
        ];
    }
}
