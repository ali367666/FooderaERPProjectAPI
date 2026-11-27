using Application.Reports;
using Xunit;

namespace Tests;

public class StoreTimeRangeTests
{
    private static readonly DateTime Day = new(2026, 11, 12);

    [Fact]
    public void Dates_without_a_time_keep_the_whole_day()
    {
        var span = ReportPeriodResolver.StoreToUtc(Day) - ReportPeriodResolver.StoreFromUtc(Day);
        Assert.Equal(TimeSpan.FromDays(1), span);
    }

    [Fact]
    public void A_time_range_ends_with_the_end_of_the_to_minute()
    {
        var span = ReportPeriodResolver.StoreToUtc(Day.AddHours(23)) - ReportPeriodResolver.StoreFromUtc(Day.AddHours(18));
        Assert.Equal(TimeSpan.FromHours(5) + TimeSpan.FromMinutes(1), span);
    }

    [Fact]
    public void A_time_before_the_day_start_belongs_to_the_night_after_the_date()
    {
        // 22:00 on the 12th until 02:00 (the night after the 12th): 4 hours and the closing minute.
        var span = ReportPeriodResolver.StoreToUtc(Day.AddHours(2)) - ReportPeriodResolver.StoreFromUtc(Day.AddHours(22));
        Assert.Equal(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(1), span);
    }

    [Fact]
    public void A_range_over_several_days_runs_from_the_first_time_to_the_last_time()
    {
        var from = ReportPeriodResolver.StoreFromUtc(Day.AddHours(18));
        var to = ReportPeriodResolver.StoreToUtc(Day.AddDays(2).AddHours(12));
        Assert.Equal(TimeSpan.FromDays(2) - TimeSpan.FromHours(6) + TimeSpan.FromMinutes(1), to - from);
    }
}
