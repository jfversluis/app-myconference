using Conference.Maui.Models;
using Conference.Maui.Models.Database;

namespace Conference.Maui.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    
    // Favorite Sessions
    Task<List<FavoriteSession>> GetAllFavoriteSessionsAsync();
    Task<FavoriteSession> GetFavoriteSessionAsync(string sessionId);
    Task<bool> SaveFavoriteSessionAsync(FavoriteSession favoriteSession);
    Task<bool> DeleteFavoriteSessionAsync(string sessionId);
    Task<bool> IsFavoriteSessionAsync(string sessionId);
    
    // Event Data Storage
    Task<List<Session>> GetAllSessionsAsync();
    Task<List<Speaker>> GetAllSpeakersAsync();
    Task<List<Room>> GetAllRoomsAsync();
    
    Task SaveEventDataAsync(List<Session> sessions, List<Speaker> speakers, List<Room> rooms);
    Task<bool> HasLocalDataAsync();
    
    // Data Hash Management
    Task<string?> GetDataHashAsync();
    Task SaveDataHashAsync(string hash);
    
    // Clear all event data (for refresh)
    Task ClearEventDataAsync();
}
