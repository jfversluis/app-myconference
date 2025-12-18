using Conference.Maui.Models;

namespace Conference.Maui.Services;

public interface ISessionizeService
{
    Task<List<Session>> GetSessionsAsync(bool forceRefresh = false);
    Task<List<Speaker>> GetSpeakersAsync(bool forceRefresh = false);
    Task<List<DaySchedule>> GetScheduleAsync(bool forceRefresh = false);
    Task<string> GetDataHashAsync();
}
