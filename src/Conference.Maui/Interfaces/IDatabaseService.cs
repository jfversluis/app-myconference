using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<FavoriteSession>> GetAllFavoriteSessionsAsync();
    Task<FavoriteSession> GetFavoriteSessionAsync(string sessionId);
    Task<bool> SaveFavoriteSessionAsync(FavoriteSession favoriteSession);
    Task<bool> DeleteFavoriteSessionAsync(string sessionId);
    Task<bool> IsFavoriteSessionAsync(string sessionId);
}
