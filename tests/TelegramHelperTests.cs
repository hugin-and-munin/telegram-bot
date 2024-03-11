namespace TelegramBot.Tests;

[TestClass]
public class TelegramHelperTests
{
    [DataTestMethod]
    [DataRow("/check 7704414297", true, 7704414297L)]
    [DataRow("/check    7704414297", true, 7704414297L)]
    [DataRow("/check 7704414297   ", true, 7704414297L)]
    [DataRow("/check7704414297", true, 7704414297L)]
    [DataRow("/check 77a04414297", false, 0L)]
    [DataRow("/check 77044142976666", false, 0L)]
    public void TryGetTinReturnsCorrectTin(string command, bool expectedResult, long expectedTin)
    {
        // Act
        var actualResult = TelegramHelper.TryGetTin(command, out var actualTin);

        // Assert
        actualResult.Should().Be(expectedResult);
        actualTin.Should().Be(expectedTin);
    }
}
