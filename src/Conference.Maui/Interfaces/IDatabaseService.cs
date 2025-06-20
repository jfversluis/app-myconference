using Conference.Maui.Models;

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
    
    // Cached Event Data
    Task<List<CachedSession>> GetCachedSessionsAsync();
    Task<List<CachedSpeaker>> GetCachedSpeakersAsync();
    Task<List<CachedRoom>> GetCachedRoomsAsync();
    Task SaveCachedSessionsAsync(List<CachedSession> sessions);
    Task SaveCachedSpeakersAsync(List<CachedSpeaker> speakers);
    Task SaveCachedRoomsAsync(List<CachedRoom> rooms);
    Task ClearCachedDataAsync();
    
    // Cache Metadata
    Task<DateTime?> GetLastCacheUpdateAsync();
    Task SetLastCacheUpdateAsync(DateTime timestamp);
    Task<bool> IsCacheExpiredAsync(TimeSpan maxAge);
}
