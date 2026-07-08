using Conference.Maui.Interfaces;
using Microsoft.Extensions.Logging;

namespace Conference.Maui.Services;

public sealed class EventTimeService : IEventTimeService
{
    private readonly IEventConfigService _configService;
    private readonly ILogger<EventTimeService> _logger;
    private TimeZoneInfo? _eventTimeZone;

    public EventTimeService(IEventConfigService configService, ILogger<EventTimeService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public TimeZoneInfo EventTimeZone => _eventTimeZone ??= ResolveEventTimeZone();

    public DateTimeOffset GetNow() => ToEventTime(DateTimeOffset.Now);

    public DateTimeOffset NormalizeSessionizeLocalTime(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Unspecified)
            return ToEventTime(new DateTimeOffset(timestamp));

        var localClockTime = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
        return new DateTimeOffset(localClockTime, EventTimeZone.GetUtcOffset(localClockTime));
    }

    public DateTimeOffset NormalizeSessionizeLocalTime(DateTimeOffset timestamp)
    {
        return NormalizeSessionizeLocalTime(timestamp.DateTime);
    }

    public DateTimeOffset ToEventTime(DateTimeOffset timestamp) => TimeZoneInfo.ConvertTime(timestamp, EventTimeZone);

    public DateOnly GetEventDate(DateTimeOffset timestamp) => DateOnly.FromDateTime(ToEventTime(timestamp).DateTime);

    public DateOnly GetEventDate(DateTime timestamp) => DateOnly.FromDateTime(NormalizeSessionizeLocalTime(timestamp).DateTime);

    public DateTimeOffset CreateEventTime(int year, int month, int day, int hour, int minute, int second = 0)
    {
        var localClockTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        return new DateTimeOffset(localClockTime, EventTimeZone.GetUtcOffset(localClockTime));
    }

    private TimeZoneInfo ResolveEventTimeZone()
    {
        var timeZoneId = _configService.Config.Event.TimeZone;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogWarning(ex, "Configured event time zone '{TimeZoneId}' was not found. Falling back to local time zone '{LocalTimeZoneId}'.", timeZoneId, TimeZoneInfo.Local.Id);
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException ex)
        {
            _logger.LogWarning(ex, "Configured event time zone '{TimeZoneId}' is invalid. Falling back to local time zone '{LocalTimeZoneId}'.", timeZoneId, TimeZoneInfo.Local.Id);
            return TimeZoneInfo.Local;
        }
    }
}
