using CredChecker;
using Grpc.Core;
using LegalEntityChecker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportProvider;
using static ReportProvider.ReportProvider;

namespace TelegramBot.Tests;

public static class TestHelpers
{
    public static (Mock<ITelegramClientAdapter>, Mock<ReportProviderClient>, TelegramBotState) GetMock()
    {
        var options = Options.Create(new AppOptions()
        {
            TelegramToken = "token",
            ReportProviderUri = "report-provider-url",
            NotificationsChatId = 111,
            NotificationsTopicId = 222,
        });
        var logger = Mock.Of<ILogger<TelegramBotState>>(MockBehavior.Strict);
        var telegramClient = new Mock<ITelegramClientAdapter>(MockBehavior.Strict);
        var reportProviderClient = new Mock<ReportProviderClient>(MockBehavior.Strict);
        reportProviderClient
            .Setup(x => x.GetGeneralInfoAsync(It.Is<ReportRequest>(x => x.Tin == 7703475603), default, default, default))
            .Returns(CreateAsyncUnaryCall(OzonGeneralInfo));
        reportProviderClient
            .Setup(x => x.GetLegalEntityInfoAsync(It.Is<ReportRequest>(x => x.Tin == 7703475603), default, default, default))
            .Returns(CreateAsyncUnaryCall(OzonLegalEntityInfo));
        var botState = new TelegramBotState(options, logger, telegramClient.Object, reportProviderClient.Object);
        return (telegramClient, reportProviderClient, botState);
    }

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(StatusCode statusCode)
    {
        var status = new Status(statusCode, string.Empty);
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(new RpcException(status)),
            Task.FromResult(new Metadata()),
            () => status,
            () => new Metadata(),
            () => { });
    }

    public static GeneralInfo YandexGeneralInfo => new()
    {
        Tin = YandexBasicInfo.Tin,
        Name = YandexBasicInfo.Name,
        Address = YandexBasicInfo.Address,
        IncorporationDate = YandexBasicInfo.IncorporationDate,
        LegalEntityStatus = YandexBasicInfo.LegalEntityStatus,
        EmployeesNumber = YandexBasicInfo.EmployeesNumber,
        AuthorizedCapital = YandexBasicInfo.AuthorizedCapital,
        Year = YandexFinanceInfo.Year,
        Profit = YandexFinanceInfo.Profit,
        Income = YandexFinanceInfo.Income,
        AccreditationState = CreditState.Credited,
        SalaryDelays = false
    };

    public static GeneralInfo SvyaznoyGeneralInfo => new()
    {
        Tin = SvyaznoyBasicInfo.Tin,
        Name = SvyaznoyBasicInfo.Name,
        Address = SvyaznoyBasicInfo.Address,
        IncorporationDate = SvyaznoyBasicInfo.IncorporationDate,
        LegalEntityStatus = SvyaznoyBasicInfo.LegalEntityStatus,
        EmployeesNumber = SvyaznoyBasicInfo.EmployeesNumber,
        AuthorizedCapital = SvyaznoyBasicInfo.AuthorizedCapital,
        Year = SvyaznoyFinanceInfo.Year,
        Profit = SvyaznoyFinanceInfo.Profit,
        Income = SvyaznoyFinanceInfo.Income,
        AccreditationState = CreditState.Unknown,
        SalaryDelays = true
    };

    public static GeneralInfo OzonGeneralInfo => new()
    {
        Tin = OzonBasicInfo.Tin,
        Name = OzonBasicInfo.Name,
        Address = OzonBasicInfo.Address,
        IncorporationDate = OzonBasicInfo.IncorporationDate,
        LegalEntityStatus = OzonBasicInfo.LegalEntityStatus,
        EmployeesNumber = OzonBasicInfo.EmployeesNumber,
        AuthorizedCapital = OzonBasicInfo.AuthorizedCapital,
        Year = OzonFinanceInfo.Year,
        Profit = OzonFinanceInfo.Profit,
        Income = OzonFinanceInfo.Income,
        AccreditationState = CreditState.Credited,
        SalaryDelays = false
    };

    public static LegalEntityInfo YandexLegalEntityInfo => new()
    {
        BasicInfo = YandexBasicInfo,
        ProceedingsInfo = YandexProceedingsInfo,
        FinanceInfo = YandexFinanceInfo,
    };

    public static LegalEntityInfo SvyaznoyLegalEntityInfo => new()
    {
        BasicInfo = SvyaznoyBasicInfo,
        ProceedingsInfo = SvyaznoyProceedingsInfo,
        FinanceInfo = SvyaznoyFinanceInfo,
    };

    public static LegalEntityInfo OzonLegalEntityInfo => new()
    {
        BasicInfo = OzonBasicInfo,
        ProceedingsInfo = OzonProceedingsInfo,
        FinanceInfo = OzonFinanceInfo,
    };

    public static BasicInfo YandexBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"ЯНДЕКС.ТЕХНОЛОГИИ\"",
                Tin = 7704414297,
                IncorporationDate = new DateTimeOffset(new DateTime(2017, 05, 19)).ToUnixTimeSeconds(),
                AuthorizedCapital = 60000000,
                EmployeesNumber = -1,
                Address = "119021,  Г.Москва, УЛ. ЛЬВА ТОЛСТОГО, Д. 16",
                LegalEntityStatus = LegalEntityStatus.Active,
                Manager = new Manager()
                {
                    Name = "МАСЮК ДМИТРИЙ ВИКТОРОВИЧ",
                    Position = "ГЕНЕРАЛЬНЫЙ ДИРЕКТОР",
                    Tin = 770373093393,
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ПУБЛИЧНАЯ КОМПАНИЯ С ОГРАНИЧЕННОЙ ОТВЕТСТВЕННОСТЬЮ \"ЯНДЕКС Н.В.\"",
                Share = 60000000,
                Size = 100,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            return result;
        }
    }

    public static ProceedingsInfo YandexProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo();
            return result;
        }
    }

    public static FinanceInfo YandexFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 50_864_263_000,
                Profit = 332_332_000,

                AccountsReceivable = 8_522_467_000,

                CapitalAndReserves = 5_246_158_000,
                LongTermLiabilities = 0,
                CurrentLiabilities = 8_222_734_000,

                PaidSalary = -34914825000
            };

            return result;
        }
    }

    public static BasicInfo OzonBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"ОЗОН ТЕХНОЛОГИИ\"",
                Tin = 7703475603,
                IncorporationDate = new DateTimeOffset(new DateTime(2019, 05, 13)).ToUnixTimeSeconds(),
                AuthorizedCapital = 10_000_000,
                EmployeesNumber = 4641,
                Address = "123112,  Г.МОСКВА, НАБ. ПРЕСНЕНСКАЯ, Д. 10, ПОМЕЩ. I, ЭТАЖ 41, КОМН. 7",
                LegalEntityStatus = LegalEntityStatus.Active,
                Manager = new Manager()
                {
                    Name = "ДЬЯЧЕНКО ВАЛЕРИЙ ВАЛЕРЬЕВИЧ",
                    Position = "ГЕНЕРАЛЬНЫЙ ДИРЕКТОР",
                    Tin = 501202997792,
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ООО \"ОЗОН ХОЛДИНГ\"",
                Share = 9_900_000,
                Size = 99,
                Tin = 7743181857,
                Type = EntityType.Company
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ООО \"ИНТЕРНЕТ РЕШЕНИЯ\"",
                Share = 100_000,
                Size = 1,
                Tin = 7704217370,
                Type = EntityType.Company
            });

            return result;
        }
    }

    public static ProceedingsInfo OzonProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo();

            return result;
        }
    }

    public static FinanceInfo OzonFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 18_646_681_000,
                Profit = 629_831_000,

                AccountsReceivable = 3_034_497_000,

                CapitalAndReserves = 630_971_000,
                LongTermLiabilities = 11_151_000,
                CurrentLiabilities = 3_276_394_000,

                PaidSalary = 15_839_326_000
            };

            return result;
        }
    }

    public static BasicInfo SvyaznoyBasicInfo
    {
        get
        {
            var result = new BasicInfo()
            {
                Name = "ООО \"СЕТЬ СВЯЗНОЙ\"",
                Tin = 7714617793,
                IncorporationDate = new DateTimeOffset(new DateTime(2005, 09, 20)).ToUnixTimeSeconds(),
                AuthorizedCapital = 32143400,
                EmployeesNumber = -1,
                Address = "123007,  Г.Москва, ПР-Д 2-Й ХОРОШЁВСКИЙ, Д. 9, К. 2, ЭТАЖ 5 КОМН 4",
                LegalEntityStatus = LegalEntityStatus.InTerminationProcess,
                Manager = new Manager()
                {
                    Name = "АНГЕЛЕВСКИ ФИЛИПП МИТРЕВИЧ",
                    Position = "КОНКУРСНЫЙ УПРАВЛЯЮЩИЙ",
                    Tin = 231906423308
                }
            };

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ДТСРЕТЕЙЛ ЛТД",
                Share = 22258645,
                Size = 69.25,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "АО \"ГРУППА КОМПАНИЙ \"СВЯЗНОЙ\"",
                Share = 1804755,
                Size = 5.61,
                Tin = 7703534714,
                Type = EntityType.Company
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "СИННАМОН ШОР ЛТД.",
                Share = 80800,
                Size = 0.25,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            result.Shareholders.Add(new Shareholder()
            {
                Name = "ЕВРОСЕТЬ ХОЛДИНГ Н.В.",
                Share = 7999200,
                Size = 24.89,
                Tin = -1,
                Type = EntityType.ForeignCompany
            });

            return result;
        }
    }

    public static ProceedingsInfo SvyaznoyProceedingsInfo
    {
        get
        {
            var result = new ProceedingsInfo()
            {
                Amount = 1843596.67,
                Count = 21,
                Description = "Оплата труда и иные выплаты по трудовым правоотношениям"
            };

            return result;
        }
    }

    public static FinanceInfo SvyaznoyFinanceInfo
    {
        get
        {
            var result = new FinanceInfo()
            {
                Year = 2022,

                Income = 56_759_628_000,
                Profit = -48_544_335_000,

                AccountsReceivable = 4_243_988_000,

                CapitalAndReserves = -44_883_153_000,
                LongTermLiabilities = 12_450_192_000,
                CurrentLiabilities = 54_133_768_000,

                PaidSalary = -6_302_428_000
            };

            return result;
        }
    }
}
