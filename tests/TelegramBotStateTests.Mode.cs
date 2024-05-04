using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using static ReportProvider.ReportProvider;

namespace TelegramBot.Tests;

[TestClass]
public partial class TelegramBotStateTests
{
    [DataTestMethod]
    [DynamicData(nameof(ModeTestData), DynamicDataSourceType.Method)]
    public async Task ModeReturnsCorrectEvent(Update update, Command expected)
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

    public static IEnumerable<object[]> ModeTestData() 
    {
        yield return 
        [
            new Update()
            {
                Message = new Message
                {
                    Text = "/mode",
                    From = new() { Id = 333 },
                    Chat = new() { Id = 333 }
                }
            },
            new SendModeSelection(new User(333),
            [
                new CallbackData("ℹ️ Oбщая информация", "mode-general-333"),
                new CallbackData("⚖️ Юридическая информация", "mode-legalinfo-333"),
                new CallbackData("🗣️ Отзывы (в разработке)", "mode-reviews-333"),
                new CallbackData("💰 Зарплаты (в разработке)", "mode-salaries-333"),
            ], 1)
        ];

        yield return 
        [
            new Update()
            {
                CallbackQuery = new()
                {
                    Data = "mode-general-333",
                    From = new() { Id = 333 },
                    Message = new()
                    {
                        Text = "mode-general-333",
                        MessageId = 999,
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new SendModeIsGeneral(new User(333), 999)
        ];

        yield return 
        [
            new Update()
            {
                CallbackQuery = new()
                {
                    Data = "mode-legalinfo-333",
                    From = new() { Id = 333 },
                    Message = new()
                    { 
                        Text = "mode-general-333",
                        MessageId = 999,
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new SendModeIsLegalEntityInfo(new User(333), 999)
        ];

        yield return 
        [
            new Update()
            {
                CallbackQuery = new()
                {
                    Data = "mode-reviews-333",
                    From = new() { Id = 333 },
                    Message = new()
                    {
                        Text = "mode-general-333",
                        MessageId = 999,
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new SendModeIsReviews(new User(333), 999)
        ];

        yield return 
        [
            new Update()
            {
                CallbackQuery = new()
                {
                    Data = "mode-salaries-333",
                    From = new() { Id = 333 },
                    Message = new()
                    {
                        Text = "mode-general-333",
                        MessageId = 999,
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new SendModeIsSalaries(new User(333), 999)
        ];
    }
}
