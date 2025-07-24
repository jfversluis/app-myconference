using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface IEventDataService
{
    Task<List<Session>> GetAllSessions();
    Task<List<Speaker>> GetAllSpeakers();
    Task<string?> GetDataHashAsync();
    Task<(List<Session> sessions, List<Speaker> speakers, List<Room> rooms)> GetAllDataAsync();
    Task<RefreshResult> RefreshDataAsync();
}
