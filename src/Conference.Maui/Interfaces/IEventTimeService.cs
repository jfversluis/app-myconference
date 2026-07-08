namespace Conference.Maui.Interfaces;

public interface IEventTimeService
{
    TimeZoneInfo EventTimeZone { get; }
    DateTimeOffset GetNow();
    DateTimeOffset NormalizeSessionizeLocalTime(DateTime timestamp);
    DateTimeOffset NormalizeSessionizeLocalTime(DateTimeOffset timestamp);
    DateTimeOffset ToEventTime(DateTimeOffset timestamp);
    DateOnly GetEventDate(DateTimeOffset timestamp);
    DateOnly GetEventDate(DateTime timestamp);
    DateTimeOffset CreateEventTime(int year, int month, int day, int hour, int minute, int second = 0);
}
