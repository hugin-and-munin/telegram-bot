using LegalEntityChecker;
using ReportProvider;

namespace TelegramBot.Tests;

[TestClass]
public class ReportConverterTests
{
    [DataTestMethod]
    [DynamicData(nameof(GeneralInfoTestData))]
    public void ConvertGeneralInfo(GeneralInfo generalInfo)
    {
        // Arrange
        var expected = File.OpenText($"./Samples/general-info-{generalInfo.Tin}.html").ReadToEnd();

        // Act
        var actual = ReportConverter.ToTelegramMessage(generalInfo);
    
        // Assert
        actual.ToString().Should().Be(expected);
    }
    
    public static IEnumerable<object[]> GeneralInfoTestData =>
    [
        [TestHelpers.YandexGeneralInfo],
        [TestHelpers.SvyaznoyGeneralInfo],
        [TestHelpers.OzonGeneralInfo],
    ];

    [DataTestMethod]
    [DynamicData(nameof(LegalEntityInfoTestData))]
    public void ConvertLegalEntityInfo(LegalEntityInfo legalEntityInfo)
    {
        // Arrange
        var expected = File.OpenText($"./Samples/legal-entity-info-{legalEntityInfo.BasicInfo.Tin}.html").ReadToEnd();

        // Act
        var actual = ReportConverter.ToTelegramMessage(legalEntityInfo);
    
        // Assert
        actual.ToString().Should().Be(expected);
    }
    
    public static IEnumerable<object[]> LegalEntityInfoTestData =>
    [
        [TestHelpers.YandexLegalEntityInfo],
        [TestHelpers.SvyaznoyLegalEntityInfo],
        [TestHelpers.OzonLegalEntityInfo],
    ];
}