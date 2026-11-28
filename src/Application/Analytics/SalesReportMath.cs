using Application.Common.Helpers;

namespace Application.Analytics;

public static class SalesReportMath
{
    private const int MaxDays = 400;

    /// <summary>Change against the earlier value in percent; null when there is nothing to compare with.</summary>
    public static decimal? Change(decimal value, decimal previous)
        => previous == 0 ? null : Math.Round((value - previous) / Math.Abs(previous) * 100, 1, MidpointRounding.AwayFromZero);

    public static decimal Percent(decimal part, decimal whole)
        => whole <= 0 ? 0 : Math.Round(part / whole * 100, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The period to compare with: the one of the same length just before — a month that starts on the 1st is compared with the month
    /// before it. While the period is still running, the earlier one is cut at the same point.
    /// </summary>
    public static (DateTime From, DateTime To) PreviousWindow(DateTime fromUtc, DateTime toUtc, DateTime nowUtc)
    {
        var until = nowUtc >= toUtc ? toUtc : nowUtc > fromUtc ? nowUtc : fromUtc;
        var length = toUtc - fromUtc;
        var local = BusinessTime.FromUtc(fromUtc);

        if (local.Day == 1 && length > TimeSpan.FromDays(1) && length <= TimeSpan.FromDays(32))
        {
            var from = fromUtc.AddMonths(-1);
            var to = until.AddMonths(-1);
            return (from, to > fromUtc ? fromUtc : to);
        }

        return (fromUtc - length, until - length);
    }

    /// <summary>Every business day from the start of the period to its end (or today, if it is still running).</summary>
    public static List<DateTime> Days(DateTime fromUtc, DateTime toUtc, DateTime nowUtc, TimeSpan dayStart)
    {
        var end = toUtc < nowUtc ? toUtc : nowUtc;
        var days = new List<DateTime>();
        if (end <= fromUtc)
            return days;

        var last = BusinessDay(end.AddTicks(-1), dayStart);
        for (var day = BusinessDay(fromUtc, dayStart); day <= last && days.Count < MaxDays; day = day.AddDays(1))
            days.Add(day);
        return days;
    }

    public static DateTime BusinessDay(DateTime utc, TimeSpan dayStart) => (BusinessTime.FromUtc(utc) - dayStart).Date;

    /// <summary>Leader = the best seller of the period; Rising = sold more than in the earlier period; otherwise Active.</summary>
    public static string WaiterStatus(int rank, decimal revenue, decimal previousRevenue)
        => rank == 1 ? "Leader" : revenue > previousRevenue ? "Rising" : "Active";

    public static string Heat(decimal revenue, decimal best)
        => revenue <= 0 || best <= 0 ? "Empty" : revenue >= best * 0.5m ? "High" : revenue >= best * 0.25m ? "Medium" : "Low";
}
