using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Infrastructure.Services;

/// <summary>
/// Service for date/time operations to allow testing
/// </summary>
public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime TodayUtc => DateTime.UtcNow.Date;

    public TimeZoneInfo UserTimeZone => TimeZoneInfo.FindSystemTimeZoneById(
        "Eastern Standard Time"); // Default to EST, would come from user preferences

    public DateTime ConvertToUserTime(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, UserTimeZone);
    }

    public DateTime ConvertToUtc(DateTime userDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(userDateTime, UserTimeZone);
    }
}
