using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    
    // Favorite sessions
    Task<List<FavoriteSession>> GetAllFavoriteSessionsAsync();
    Task<FavoriteSession> GetFavoriteSessionAsync(string sessionId);
    Task<bool> SaveFavoriteSessionAsync(FavoriteSession favoriteSession);
    Task<bool> DeleteFavoriteSessionAsync(string sessionId);
    Task<bool> IsFavoriteSessionAsync(string sessionId);
    
    // Cached event data
    Task<List<CachedSession>> GetAllCachedSessionsAsync();
    Task<List<CachedSpeaker>> GetAllCachedSpeakersAsync();
    Task<List<CachedRoom>> GetAllCachedRoomsAsync();
    
    Task SaveCachedSessionsAsync(List<CachedSession> sessions);
    Task SaveCachedSpeakersAsync(List<CachedSpeaker> speakers);
    Task SaveCachedRoomsAsync(List<CachedRoom> rooms);
    
    Task ClearCachedDataAsync();
    
    // Data cache info
    Task<DataCacheInfo?> GetDataCacheInfoAsync(string key);
    Task SaveDataCacheInfoAsync(DataCacheInfo cacheInfo);
    
    // Check if cached data exists
    Task<bool> HasCachedDataAsync();
}
