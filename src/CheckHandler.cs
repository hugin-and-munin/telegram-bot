using System.Text;
using CredChecker;
using LegalEntityChecker;
using static ReportProvider.ReportProvider;

namespace Bot;

public class CheckHandler(ReportProviderClient _reportProvider)
{
    public async Task<string> Handle(long tin, CancellationToken ct)
    {
        var request = new ReportProvider.ReportRequest() { Tin = tin };

        var report = await _reportProvider.GetAsync(request, cancellationToken: ct);

        if (report is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendFormat("<b>{0}</b>", report.Name).AppendLine().AppendLine();

        // Сервис данных юридических лиц
        sb.AppendFormat("<b>Основная информация</b>").AppendLine().AppendLine();
        sb.AppendFormat("ИНН: <code>{0}</code>", report.Tin).AppendLine();
        sb.AppendFormat("Дата регистрации: {0:yyyy-MM-dd}", DateTimeOffset.FromUnixTimeSeconds(report.IncorporationDate)).AppendLine();
        sb.AppendFormat("Уставный капитал: {0:N0} ₽", report.AuthorizedCapital).AppendLine();
        if (report.EmployeesNumber == -1)
        {
            sb.AppendLine("Количество сотрудников: нет данных");
        }
        else
        {
            sb.AppendFormat("Количество сотрудников: {0:N0}", report.EmployeesNumber).AppendLine();
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
        sb.AppendFormat("Аккредитация Минфры: {0}", report.AccreditationState switch
        {
            CreditState.Unknown => "❔Нет данных",
            CreditState.Credited => "✅ Аккредитована",
            CreditState.NotCredited => "❌ Нет аккредитации",
            _ => throw new NotSupportedException($"{report.AccreditationState} not supported"),
        }).AppendLine();

        // Сервис отзывов
        sb.AppendLine().AppendFormat("<b>Отзывы</b>").AppendLine().AppendLine();
        sb.AppendFormat("{0} негативных отзывов", "x").AppendLine();

        // Сервис зарплат
        sb.AppendLine().AppendFormat("<b>Зарплата</b>").AppendLine().AppendLine();
        sb.AppendFormat("Медиана: {0}", "TODO");

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("<a href=\"https://github.com/hugin-and-munin\">GitHub</a> | <a href=\"https://t.me/it_hugin_and_munin\">Telegram</a>");

        return sb.ToString();
    }
}
