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

public class ReportWindowTimeTests
{
    private static readonly Application.Reports.Dtos.ReportWindow Day = ReportPeriodResolver.Resolve(
        new BusinessReportRequest { Period = ReportPeriod.Custom, From = new DateTime(2026, 11, 4), To = new DateTime(2026, 11, 4) },
        new DateTime(2026, 11, 5, 12, 0, 0), TimeSpan.Zero);

    [Fact]
    public void Without_times_the_window_is_the_whole_day()
    {
        var window = ReportPeriodResolver.WithTimes(Day, null, null);

        Assert.Equal(new DateTime(2026, 11, 4, 0, 0, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 5, 0, 0, 0), window.ToLocal);
    }

    [Fact]
    public void A_time_range_narrows_the_day_and_includes_the_last_minute()
    {
        var window = ReportPeriodResolver.WithTimes(Day, TimeSpan.FromHours(14), new TimeSpan(18, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 4, 14, 0, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 4, 18, 31, 0), window.ToLocal);
    }

    [Fact]
    public void Only_a_start_time_keeps_the_end_of_the_day()
    {
        var window = ReportPeriodResolver.WithTimes(Day, TimeSpan.FromHours(20), null);

        Assert.Equal(new DateTime(2026, 11, 4, 20, 0, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 5, 0, 0, 0), window.ToLocal);
    }

    [Fact]
    public void A_date_that_carries_a_time_counts_as_that_time()
    {
        var window = ReportPeriodResolver.WithTimes(Day, null, null, from: new DateTime(2026, 11, 4, 9, 15, 0), to: new DateTime(2026, 11, 4, 11, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 4, 9, 15, 0), window.FromLocal);
        Assert.Equal(new DateTime(2026, 11, 4, 11, 1, 0), window.ToLocal);
    }

    [Fact]
    public void An_end_before_the_start_is_refused()
        => Assert.Throws<Application.Common.Exceptions.BadRequestException>(
            () => ReportPeriodResolver.WithTimes(Day, TimeSpan.FromHours(18), TimeSpan.FromHours(10)));
}
