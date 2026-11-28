using System.Globalization;
using System.Text;
using Application.Finance.Dtos;

namespace Application.Finance;

/// <summary>The finance report as a CSV file Excel opens directly.</summary>
public static class FinanceReportCsv
{
    public static byte[] Build(FinanceReportResponse report)
    {
        static string Number(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
        static string Day(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        // A cell a spreadsheet cannot read as a formula or as two columns.
        static string Text(string? value)
        {
            var text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 0 && "=+-@\t".Contains(text[0]))
                text = "'" + text;
            return text.Contains(';') || text.Contains('"') ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
        }

        var csv = new StringBuilder();
        csv.AppendLine($"Dövr;{Day(report.FromDate)};{Day(report.ToDate)}");
        csv.AppendLine($"Ümumi gəlir;{Number(report.TotalIncome)}");
        csv.AppendLine($"Ümumi xərc;{Number(report.TotalExpense)}");
        csv.AppendLine($"Mənfəət;{Number(report.Profit)}");
        csv.AppendLine($"Mənfəət marjası %;{Number(report.ProfitMarginPercent)}");
        csv.AppendLine();
        csv.AppendLine("Dövr;Gəlir;Xərc;Mənfəət");
        foreach (var b in report.Buckets)
            csv.AppendLine($"{Text(b.Label)};{Number(b.Income)};{Number(b.Expense)};{Number(b.Profit)}");
        csv.AppendLine();
        csv.AppendLine("Tarix;Növ;Ad;Məbləğ;Qeyd");
        foreach (var day in report.Days)
        {
            foreach (var line in day.IncomeLines)
                csv.AppendLine($"{Day(day.Date)};Gəlir;{Text(line.Name)};{Number(line.Amount)};{Text(line.Note)}");
            foreach (var line in day.ExpenseLines)
                csv.AppendLine($"{Day(day.Date)};Xərc;{Text(line.Name)};{Number(line.Amount)};{Text(line.Note)}");
        }

        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
    }
}
