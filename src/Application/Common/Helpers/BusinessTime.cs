namespace Application.Common.Helpers;

/// <summary>
/// Business-local (Baku) time for anything a person reads or compares against wall-clock hours —
/// printed tickets, opening time, the date part of the rotating POS PIN. Storage stays UTC.
/// Independent of the server's own time zone, so a UTC container behaves the same as a local PC.
/// </summary>
public static class BusinessTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Baku", "Azerbaijan Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Azerbaijan has had no DST since 2016 — a fixed UTC+4 is exact.
        return TimeZoneInfo.CreateCustomTimeZone("Baku", TimeSpan.FromHours(4), "Baku", "Baku");
    }
}
