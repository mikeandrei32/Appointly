namespace Appointly.Api.Services;

public static class ClinicTime
{
    public const int DurationMinutes = 30;
    public const string TimezoneId = "Asia/Manila";
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    public static DateTime ToUtc(DateOnly date, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time, DateTimeKind.Unspecified), Zone);

    public static bool IsGridAligned(TimeOnly time) =>
        time.Ticks % TimeSpan.FromMinutes(DurationMinutes).Ticks == 0;
}
