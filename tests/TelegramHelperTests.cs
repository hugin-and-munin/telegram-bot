namespace TelegramBot.Tests;

[TestClass]
public class TelegramHelperTests
{
    [DataTestMethod]
    [DataRow("/check@it_hugin_and_munin_bot 7704414297", true, 7704414297L)]
    [DataRow("/check@it_hugin_and_munin_bot7704414297", true, 7704414297L)]
    [DataRow("/check 7704414297", true, 7704414297L)]
    [DataRow("/check    7704414297", true, 7704414297L)]
    [DataRow("/check 7704414297   ", true, 7704414297L)]
    [DataRow("/check7704414297", true, 7704414297L)]
    [DataRow("/check 77a04414297", false, -1)]
    [DataRow("/check 77044142976666", false, -1)]
    [DataRow("/check 0", false, -1)]
    [DataRow("/check 123", false, -1)]
    [DataRow("/check 0000000000", false, -1)]
    [DataRow("/check -1234567890", false, -1)]
    [DataRow("/check 92233720368547758079", false, -1)]
    [DataRow("/check -92233720368547758079", false, -1)]
    [DataRow("/check            ", false, -1)]
    public void TryGetTinReturnsCorrectTin(string command, bool expectedResult, long expectedTin)
    {
        // Act
        var actualResult = TelegramHelper.TryGetTin(command, out var actualTin);

        // Assert
        actualResult.Should().Be(expectedResult);
        actualTin.Should().Be(expectedTin);
    }
}
