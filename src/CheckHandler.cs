using System.Globalization;
using System.Text;
using CredChecker;
using LegalEntityChecker;
using static ReportProvider.ReportProvider;

namespace TelegramBot;

public class CheckHandler(ReportProviderClient _reportProvider)
{
    public async Task<StringBuilder?> Handle(long tin, CancellationToken ct)
    {
        var request = new ReportProvider.ReportRequest() { Tin = tin };

        var report = await _reportProvider.GetAsync(request, cancellationToken: ct);

        if (report is null) return null;

        var sb = new StringBuilder();
        sb.AppendFormat("<b>{0}</b>", report.Name).AppendLine().AppendLine();

        // Основная информация
        sb.AppendFormat("<b>ℹ️ Основная информация</b>").AppendLine().AppendLine();
        sb.AppendFormat("ИНН: <code>{0}</code>", report.Tin).AppendLine();
        sb.AppendFormat("Дата регистрации: {0:yyyy-MM-dd}", DateTimeOffset.FromUnixTimeSeconds(report.IncorporationDate)).AppendLine();
        sb.AppendFormat(CultureInfo.GetCultureInfo("ru-RU"), "Уставный капитал: {0:N0} ₽", report.AuthorizedCapital).AppendLine();
        
        if (report.EmployeesNumber > 0)
        {
            sb.AppendFormat(CultureInfo.GetCultureInfo("ru-RU"), "Количество сотрудников: {0:N0}", report.EmployeesNumber).AppendLine();
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

        // Отзывы
        sb.AppendLine().AppendFormat("<b>🗣️ Отзывы</b>").AppendLine().AppendLine();
        sb.AppendFormat("{0}", "TODO").AppendLine();

        // Зарплаты
        sb.AppendLine().AppendFormat("<b>💰 Зарплата</b>").AppendLine().AppendLine();
        sb.AppendFormat("{0}", "TODO").AppendLine();

        return sb;
    }
}
