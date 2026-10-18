using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Reports.Dtos;
using Domain.Enums;

namespace Application.Reports;

/// <summary>
/// Turns "today / this week / this month / custom" into concrete bounds. A business day runs from
/// 06:00 to 06:00 the next morning (as on the printed Z report), so a sale rung up at 02:00 still
/// belongs to the previous day.
/// </summary>
public static class ReportPeriodResolver
{
    public static readonly TimeSpan DayStart = TimeSpan.FromHours(6);
    private const int MaxCustomDays = 366;

    public static ReportWindow Resolve(BusinessReportRequest request) => Resolve(request, BusinessTime.Now);

    public static ReportWindow Resolve(BusinessReportRequest request, DateTime nowLocal)
    {
        var businessDate = (nowLocal - DayStart).Date;

        DateTime fromDate;
        DateTime toDateExclusive;

        switch (request.Period)
        {
            case ReportPeriod.Today:
                fromDate = businessDate;
                toDateExclusive = businessDate.AddDays(1);
                break;

            case ReportPeriod.ThisWeek:
                fromDate = businessDate.AddDays(-(((int)businessDate.DayOfWeek + 6) % 7));
                toDateExclusive = fromDate.AddDays(7);
                break;

            case ReportPeriod.ThisMonth:
                fromDate = new DateTime(businessDate.Year, businessDate.Month, 1);
                toDateExclusive = fromDate.AddMonths(1);
                break;

            case ReportPeriod.Custom:
                if (request.From is null)
                    throw new BadRequestException("Özel tarix üçün başlanğıc tarix seçilməlidir.");
                fromDate = request.From.Value.Date;
                var lastDate = (request.To ?? request.From).Value.Date;
                if (lastDate < fromDate)
                    throw new BadRequestException("Bitmə tarixi başlanğıc tarixindən əvvəl ola bilməz.");
                toDateExclusive = lastDate.AddDays(1);
                if ((toDateExclusive - fromDate).TotalDays > MaxCustomDays)
                    throw new BadRequestException($"Tarix aralığı ən çox {MaxCustomDays} gün ola bilər.");
                break;

            default:
                throw new BadRequestException("Hesabat dövrü yanlışdır.");
        }

        var fromLocal = fromDate + DayStart;
        var toLocal = toDateExclusive + DayStart;
        return new ReportWindow(fromLocal, toLocal, BusinessTime.ToUtc(fromLocal), BusinessTime.ToUtc(toLocal));
    }
}
