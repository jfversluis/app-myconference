namespace Conference.Maui.Interfaces;

/// <summary>
/// Service for managing user's favorite sessions.
/// </summary>
public interface IFavoritesService
{
    /// <summary>
    /// Gets a snapshot of all favorited session IDs.
    /// </summary>
    Task<IReadOnlySet<string>> GetFavoriteSessionIdsAsync();

    /// <summary>
    /// Checks if a session is favorited.
    /// </summary>
    Task<bool> IsFavoriteAsync(string sessionId);

    /// <summary>
    /// Toggles the favorite status of a session.
    /// </summary>
    Task<bool> ToggleFavoriteAsync(string sessionId);

    /// <summary>
    /// Adds a session to favorites.
    /// </summary>
    Task AddFavoriteAsync(string sessionId);

    /// <summary>
    /// Removes a session from favorites.
    /// </summary>
    Task RemoveFavoriteAsync(string sessionId);
}
