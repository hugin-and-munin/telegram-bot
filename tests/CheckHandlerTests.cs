using CredChecker;
using LegalEntityChecker;
using ReportProvider;
using static ReportProvider.ReportProvider;

namespace TelegramBot.Tests;

[TestClass]
public class CheckHandlerTests
{
    [DataTestMethod]
    [DynamicData(nameof(ReportResponses))]
    public async Task ReportProviderReturnsCorrectReport(string path, ReportReponse expected)
    {
        // Arrange
        var tin = expected.Tin;
        var expectedReport = File.OpenText(path).ReadToEnd();
        var reportProviderCall = TestHelpers.CreateAsyncUnaryCall(expected);
        var reportProviderClientMock = new Mock<ReportProviderClient>();
        reportProviderClientMock
            .Setup(x => x.GetAsync(It.Is<ReportRequest>(r => r.Tin == tin), default, default, default))
            .Returns(reportProviderCall);
        var sut = new CheckHandler(reportProviderClientMock.Object);

        // Act
        var actualReport = await sut.Handle(tin, default);

        // Assert
        reportProviderClientMock.Verify(x => x.GetAsync(It.Is<ReportRequest>(r => r.Tin == tin), default, default, default), Times.Once);
        actualReport!.ToString().Should().Be(expectedReport);
    }

    public static IEnumerable<object[]> ReportResponses =>
    [
        [ "./Samples/yandex.html", YandexReportResponse ],
        [ "./Samples/svyaznoy.html", SvyaznoyReportResponse ],
    ];

    public static ReportReponse YandexReportResponse => new()
    {
        Tin = 7704414297,
        Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
        Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
        AuthorizedCapital = 60000000,
        EmployeesNumber = 0,
        IncorporationDate = new DateTimeOffset(2017, 05, 19, 0, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.Active,
        AccreditationState = CreditState.Credited,
    };

    public static ReportReponse SvyaznoyReportResponse => new()
    {
        Tin = 7714617793,
        Name = "ООО \"СЕТЬ СВЯЗНОЙ\"",
        Address = "123007,  Г.Москва, ПР-Д 2-Й ХОРОШЁВСКИЙ, Д. 9, К. 2, ЭТАЖ 5 КОМН 4",
        AuthorizedCapital = 32143400,
        EmployeesNumber = -1,
        IncorporationDate = new DateTimeOffset(2005, 09, 20, 0, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.InTerminationProcess,
        SalaryDelays = true
    };
}
