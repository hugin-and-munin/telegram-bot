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

        // Сервис данных юридических лиц
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
        sb.AppendFormat("Юридический адрес: <a href=\"https://yandex.com/maps?text={0}\">{1}</a>", report.Address.Replace(' ', '.'), report.Address).AppendLine();
        sb.AppendFormat("Статус компании: {0}", report.LegalEntityStatus switch
        {
            LegalEntityStatus.Active => "Действующая",
            LegalEntityStatus.Bankruptcy => "❗В процессе банкротства",
            LegalEntityStatus.InReorganizationProcess => "❔В процессе реорганизации",
            LegalEntityStatus.InTerminationProcess => "❗В процессе ликвидации",
            LegalEntityStatus.Terminated => "❗Ликвидирована",
            _ => throw new NotSupportedException($"{report.LegalEntityStatus} not supported"),
        }).AppendLine();

        // Сервис аккредитации
        sb.AppendFormat("Аккредитация Минцифры: {0}", report.AccreditationState switch
        {
            CreditState.Unknown => "❔Нет данных",
            CreditState.Credited => "✅ Аккредитована",
            CreditState.NotCredited => "❌ Нет аккредитации",
            _ => throw new NotSupportedException($"{report.AccreditationState} not supported"),
        }).AppendLine();

        // Сервис отзывов
        sb.AppendLine().AppendFormat("<b>🗣️ Отзывы</b>").AppendLine().AppendLine();
        sb.AppendFormat("{0}", "TODO").AppendLine();

        // Сервис зарплат
        sb.AppendLine().AppendFormat("<b>💲 Зарплата</b>").AppendLine().AppendLine();
        sb.AppendFormat("{0}", "TODO").AppendLine();

        return sb;
    }
}
