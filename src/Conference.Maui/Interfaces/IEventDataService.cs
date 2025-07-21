using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface IEventDataService
{
    Task<List<Session>> GetAllSessions();
    Task<List<Speaker>> GetAllSpeakers();
    
    // New caching and refresh methods
    Task<bool> HasCachedDataAsync();
    Task<List<Session>> GetCachedSessionsAsync();
    Task<List<Speaker>> GetCachedSpeakersAsync();
    Task RefreshDataAsync(bool forceRefresh = false);
    Task<bool> IsRefreshingAsync();
    
    // Events for data updates
    event EventHandler<EventArgs>? DataRefreshed;
    event EventHandler<bool>? RefreshStateChanged;
}
