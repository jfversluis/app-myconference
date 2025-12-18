namespace Conference.Maui.Services;

public interface IFavoritesService
{
    Task<HashSet<string>> GetFavoritesAsync();
    Task AddFavoriteAsync(string sessionId);
    Task RemoveFavoriteAsync(string sessionId);
    Task<bool> IsFavoriteAsync(string sessionId);
}
