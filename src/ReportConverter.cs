using System.Globalization;
using System.Text;
using CredChecker;
using LegalEntityChecker;
using ReportProvider;

namespace TelegramBot;

public static class ReportConverter
{
    private static readonly CultureInfo ruCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static StringBuilder ToTelegramMessage(GeneralInfo report)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("<b>{0}</b>", report.Name).AppendLine().AppendLine();

        // Основная информация
        sb.AppendFormat("<b>ℹ️ Основная информация</b>").AppendLine().AppendLine();
        sb.AppendFormat("ИНН: <code>{0}</code>", report.Tin).AppendLine();
        sb.AppendFormat("Дата регистрации: {0:yyyy-MM-dd}", DateTimeOffset.FromUnixTimeSeconds(report.IncorporationDate)).AppendLine();
        sb.AppendFormat(ruCulture, "Уставный капитал: {0:N0} ₽", report.AuthorizedCapital).AppendLine();

        if (report.EmployeesNumber > 0)
        {
            sb.AppendFormat(ruCulture, "Количество сотрудников: {0:N0}", report.EmployeesNumber).AppendLine();
        }
        else
        {
            sb.AppendLine("Количество сотрудников: нет данных");
        }

        sb.AppendFormat("📍 <a href=\"https://yandex.com/maps?text={0}\">{1}</a>", report.Address.Replace(' ', '.'), report.Address).AppendLine();

        if (report.AccreditationState == CreditState.Credited)
        {
            sb.AppendLine("✅ Аккредитация Минцифры");
        }
        sb.AppendFormat(report.Profit > 0 ? "Прибыль за {0} год: {1:N0} ₽" : "⚠️ Убыток за {0} год: {1:N0} ₽", report.Year, report.Profit).AppendLine();

        // Негативные сведения
        if (report.LegalEntityStatus != LegalEntityStatus.Active ||
            report.AccreditationState != CreditState.Credited ||
            report.SalaryDelays)
        {
            sb.AppendLine().AppendFormat("<b>⚠️ Негативные сведения</b>").AppendLine().AppendLine();

            if (report.AccreditationState != CreditState.Credited)
            {
                sb.AppendLine("❗️Нет аккредитации Минцифры");
            }

            if (report.LegalEntityStatus == LegalEntityStatus.Bankruptcy)
            {
                sb.AppendLine("❗️Компания в процессе банкротства");
            }
            else if (report.LegalEntityStatus == LegalEntityStatus.InReorganizationProcess)
            {
                sb.AppendLine("❗️Компания в процессе реорганизации");
            }
            else if (report.LegalEntityStatus == LegalEntityStatus.InTerminationProcess)
            {
                sb.AppendLine("❗️Компания в процессе ликвидации");
            }
            else if (report.LegalEntityStatus == LegalEntityStatus.Terminated)
            {
                sb.AppendLine("❗️Компания ликвидирована");
            }

            if (report.SalaryDelays) sb.AppendLine("❗️Задерживают зарплату");
        }

        return sb;
    }

    public static StringBuilder ToTelegramMessage(LegalEntityInfo report)
    {
        var sb = new StringBuilder();

        var basicInfo = report.BasicInfo;
        var financeInfo = report.FinanceInfo;
        var proceedingsInfo = report.ProceedingsInfo;

        sb.AppendFormat("<b>{0}</b>", basicInfo.Name).AppendLine().AppendLine();

        // Основная информация
        sb.AppendFormat("<b>⚖️ Юридическая информация</b>").AppendLine().AppendLine();
        sb.AppendFormat("ИНН: <code>{0}</code>", basicInfo.Tin).AppendLine();
        sb.AppendFormat("Дата регистрации: {0:yyyy-MM-dd}", DateTimeOffset.FromUnixTimeSeconds(basicInfo.IncorporationDate)).AppendLine();
        sb.AppendFormat(ruCulture, "Уставный капитал: {0:N0} ₽", basicInfo.AuthorizedCapital).AppendLine();

        if (basicInfo.EmployeesNumber > 0)
        {
            sb.AppendFormat(ruCulture, "Количество сотрудников: {0:N0}", basicInfo.EmployeesNumber).AppendLine();
        }
        else
        {
            sb.AppendLine("Количество сотрудников: нет данных");
        }

        sb.AppendFormat("Адрес: <a href=\"https://yandex.com/maps?text={0}\">{1}</a>", basicInfo.Address.Replace(' ', '.'), basicInfo.Address).AppendLine();

        var statusString = basicInfo.LegalEntityStatus switch
        {
            LegalEntityStatus.Active => "Действующая компания",
            LegalEntityStatus.Bankruptcy => "❗️Компания в процессе банкротства",
            LegalEntityStatus.InReorganizationProcess => "❗️Компания в процессе реорганизации",
            LegalEntityStatus.InTerminationProcess => "❗️Компания в процессе ликвидации",
            LegalEntityStatus.Terminated => "❗️Компания ликвидирована",
            _ => throw new NotImplementedException(),
        };

        sb.AppendFormat("Статус: {0}", statusString).AppendLine().AppendLine();

        sb.AppendLine("<b>👤 Руководитель</b>").AppendLine();
        sb.AppendFormat("Должность: {0}", ruCulture.TextInfo.ToTitleCase(basicInfo.Manager.Position.ToLower())).AppendLine();
        sb.AppendFormat("Имя: {0}", ruCulture.TextInfo.ToTitleCase(basicInfo.Manager.Name.ToLower())).AppendLine();
        sb.AppendFormat("ИНН: <code>{0}</code>", basicInfo.Manager.Tin).AppendLine().AppendLine();

        sb.AppendLine("<b>💼 Учредители</b>").AppendLine();

        foreach (var shareholder in basicInfo.Shareholders)
        {
            sb.AppendFormat("{0}", shareholder.Name).AppendLine();
            if (shareholder.Tin > 0) sb.AppendFormat("ИНН: <code>{0}</code>", shareholder.Tin).AppendLine();
            else sb.AppendFormat("ИНН: отсутствует (иностранное юрлицо)", shareholder.Tin).AppendLine();
            sb.AppendFormat("Доля: {0:N0} ₽ ({1:N2})", shareholder.Share, shareholder.Size).AppendLine().AppendLine();
        }

        sb.AppendFormat("<b>📈 Финансовая информация за {0} год</b>", financeInfo.Year).AppendLine().AppendLine();

        sb.AppendFormat("Доходы: {0:N0} ₽", financeInfo.Income).AppendLine();
        sb.AppendFormat(financeInfo.Profit > 0 ? "Прибыль: {0:N0} ₽" : "⚠️ Убыток: {0:N0} ₽", financeInfo.Profit).AppendLine();
        sb.AppendFormat("Дебиторская задолженность: {0:N0} ₽", financeInfo.AccountsReceivable).AppendLine();
        sb.AppendFormat("Капитал и резервы: {0:N0} ₽", financeInfo.CapitalAndReserves).AppendLine();
        sb.AppendFormat("Долгосрочные обязательства: {0:N0} ₽", financeInfo.LongTermLiabilities).AppendLine();
        sb.AppendFormat("Краткосрочные обязательства: {0:N0} ₽", financeInfo.CurrentLiabilities).AppendLine();
        sb.AppendFormat("Платежи на оплату труда работников: {0:N0} ₽", financeInfo.PaidSalary).AppendLine();

        if (proceedingsInfo.Count > 0)
        {
            sb.AppendFormat("⚠️ Есть долг по зарплате перед сотрудниками: {0}", proceedingsInfo.Amount).AppendLine();
        }

        return sb;
    }
}