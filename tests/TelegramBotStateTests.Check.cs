using Telegram.Bot.Types;

namespace TelegramBot.Tests;

public partial class TelegramBotStateTests
{
    [TestMethod]
    public async Task OnCheckWithoutSelectedModeForceUserToSelectTheMode()
    {
        // Arrange
        var (_, reportProviderMock, botState) = TestHelpers.GetMock();
        var update = new Update()
        {
            Message = new Message
            {
                Text = "/check 7703475603",
                From = new() { Id = 333 },
                Chat = new() { Id = 333 }
            }
        };
        var expected =  new SendModeSelection(new User(333), 
        [
            new CallbackData("ℹ️ Oбщая информация", "mode-general-333"),
            new CallbackData("⚖️ Юридическая информация", "mode-legalinfo-333"),
            new CallbackData("🗣️ Отзывы (в разработке)", "mode-reviews-333"),
            new CallbackData("💰 Зарплаты (в разработке)", "mode-salaries-333"),
        ], 1);

        // Act
        Command actual = await botState.GetEvent(update, CancellationToken.None);

        // Assert
        actual.Should().BeOfType(expected.GetType());
        actual.Should().BeEquivalentTo(expected, o => o.RespectingRuntimeTypes());
    }

    [DataTestMethod]
    [DynamicData(nameof(CheckTestData), DynamicDataSourceType.Method)]
    public async Task CheckReturnsCorrectReport(Update setMode, Update check, Command expected)
    {
        // Arrange
        var (_, reportProviderMock, botState) = TestHelpers.GetMock();

        // Act
        await botState.GetEvent(setMode, CancellationToken.None);
        Command actual = await botState.GetEvent(check, CancellationToken.None);

        // Assert
        actual.Should().BeOfType(expected.GetType());
        actual.Should().BeEquivalentTo(expected, o => o.RespectingRuntimeTypes());
    }

    public static IEnumerable<object[]> CheckTestData() 
    {
        // On check specific company in general mode, 
        // returns general report
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
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new Update()
            {
                Message = new Message
                {
                    Text = "/check 7703475603",
                    From = new() { Id = 333 },
                    Chat = new() { Id = 333 }
                }
            },
            new SendGeneralReport(
                new User(333), 
                Report: System.IO.File.OpenText("./Samples/general-info-7703475603.html").ReadToEnd())
        ];

        // On check specific company in legal info mode, 
        // returns legal info report
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
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new Update()
            {
                Message = new Message
                {
                    Text = "/check 7703475603",
                    From = new() { Id = 333 },
                    Chat = new() { Id = 333 }
                }
            },
            new SendLegalEntityReport(
                new User(333), 
                Report: System.IO.File.OpenText("./Samples/legal-entity-info-7703475603.html").ReadToEnd(),
                Data: 
                [
                    new CallbackData(@"ООО ""ОЗОН ХОЛДИНГ""", "check-333-7743181857"),
                    new CallbackData(@"ООО ""ИНТЕРНЕТ РЕШЕНИЯ""", "check-333-7704217370"),
                ],
                ButtonsPerRow: 1)
        ];

        // On check specific company from callback in legal info mode, 
        // returns legal info report
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
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new Update()
            {
                CallbackQuery = new()
                {
                    Data = "check-7703475603-333",
                    From = new() { Id = 333 },
                    Message = new()
                    { 
                        Text = "mode-general-333",
                        Chat = new() { Id = 333 },
                        From = new() { Id = 333 }
                    }
                }
            },
            new SendLegalEntityReport(
                new User(333), 
                Report: System.IO.File.OpenText("./Samples/legal-entity-info-7703475603.html").ReadToEnd(),
                Data: 
                [
                    new CallbackData(@"ООО ""ОЗОН ХОЛДИНГ""", "check-333-7743181857"),
                    new CallbackData(@"ООО ""ИНТЕРНЕТ РЕШЕНИЯ""", "check-333-7704217370"),
                ],
                ButtonsPerRow: 1)
        ];
    }
}