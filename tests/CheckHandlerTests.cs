using CredChecker;
using LegalEntityChecker;
using ReportProvider;
using static ReportProvider.ReportProvider;

namespace TelegramBot.Tests;

[TestClass]
public class CheckHandlerTests
{
    [TestMethod]
    public async Task ReportProviderReturnsCorrectReport()
    {
        // Arrange
        var tin = 7704414297L;
        var expectedReport = File.OpenText("./Samples/report.html").ReadToEnd();
        var reportProviderCall = TestHelpers.CreateAsyncUnaryCall(ExpectetReportResponse);
        var reportProviderClientMock = new Mock<ReportProviderClient>();
        reportProviderClientMock
            .Setup(x => x.GetAsync(It.Is<ReportRequest>(r => r.Tin == tin), default, default, default))
            .Returns(reportProviderCall);
        var sut = new CheckHandler(reportProviderClientMock.Object);

        // Act
        var actualReport = await sut.Handle(tin, default);

        // Assert
        reportProviderClientMock.Verify(x => x.GetAsync(It.Is<ReportRequest>(r => r.Tin == tin), default, default, default), Times.Once);
        actualReport.Should().Be(expectedReport);
    }

    public static ReportReponse ExpectetReportResponse => new()
    {
        Tin = 7704414297,
        Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
        Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
        AuthorizedCapital = 60000000,
        EmployeesNumber = -1,
        IncorporationDate = new DateTimeOffset(2017, 05, 19, 0, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        LegalEntityStatus = LegalEntityStatus.Active,
        AccreditationState = CreditState.Credited
    };
}
