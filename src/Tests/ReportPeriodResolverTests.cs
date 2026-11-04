using Application.Reports;
using Application.Reports.Dtos;
using Domain.Enums;
using Xunit;

namespace Tests;

public class ReportPeriodResolverTests
{
    private static readonly BusinessReportRequest Today = new() { Period = ReportPeriod.Today };

    [Fact]
    public void Company_working_in_shifts_keeps_the_06_00_business_day()
    {
        // 03:00 on the 4th still belongs to the business day that began at 06:00 on the 3rd.
        var window = ReportPeriodResolver.Resolve(Today, new DateTime(2026, 11, 4, 3, 0, 0), ReportPeriodResolver.DayStartFor(new Domain.Entities.CompanySettings()));

        Assert.Equal(new DateTime(2026, 11, 3, 6, 0, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 4, 6, 0, 0), window.ToLocal);
    }

    [Fact]
    public void Company_working_by_the_day_reports_the_calendar_day_including_the_small_hours()
    {
        var settings = new Domain.Entities.CompanySettings { OpeningTime = TimeSpan.FromHours(8) };
        var window = ReportPeriodResolver.Resolve(Today, new DateTime(2026, 11, 4, 3, 0, 0), ReportPeriodResolver.DayStartFor(settings));

        Assert.Equal(new DateTime(2026, 11, 4, 0, 0, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 5, 0, 0, 0), window.ToLocal);
    }
}
