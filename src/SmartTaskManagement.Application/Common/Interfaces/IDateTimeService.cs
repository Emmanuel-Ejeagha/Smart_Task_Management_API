namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Service for date/time operations to allow testing
/// </summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime TodayUtc { get; }
    TimeZoneInfo UserTimeZone { get; }
    DateTime ConvertToUserTime(DateTime utcDateTime);
    DateTime ConvertToUtc(DateTime userDateTime);
}