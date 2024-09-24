using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface IEventDataService
{
    Task<List<Session>> GetAllSessions();
}
