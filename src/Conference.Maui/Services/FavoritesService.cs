using System.Reactive.Linq;
using Akavache;
using Conference.Maui.Interfaces;
using Microsoft.Extensions.Logging;

namespace Conference.Maui.Services;

public class FavoritesService : IFavoritesService
{
    private readonly IBlobCache _cache;
    private readonly ILogger<FavoritesService> _logger;
    private HashSet<string>? _favorites;
    private const string FavoritesCacheKey = "user_favorites";

    public event EventHandler<string>? FavoritesChanged;

    public FavoritesService(ILogger<FavoritesService> logger)
    {
        _cache = BlobCache.UserAccount;
        _logger = logger;
    }

    public async Task<HashSet<string>> GetFavoriteSessionIdsAsync()
    {
        if (_favorites != null)
            return _favorites;

        try
        {
            _favorites = await _cache.GetObject<HashSet<string>>(FavoritesCacheKey);
            _logger.LogDebug("Loaded {Count} favorites from cache", _favorites.Count);
        }
        catch (KeyNotFoundException)
        {
            _favorites = [];
            _logger.LogDebug("No favorites found, starting with empty set");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading favorites");
            _favorites = [];
        }

        return _favorites;
    }

    public async Task<bool> IsFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoriteSessionIdsAsync();
        return favorites.Contains(sessionId);
    }

    public async Task<bool> ToggleFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoriteSessionIdsAsync();
        
        bool isFavorite;
        if (favorites.Contains(sessionId))
        {
            favorites.Remove(sessionId);
            isFavorite = false;
            _logger.LogDebug("Removed session {SessionId} from favorites", sessionId);
        }
        else
        {
            favorites.Add(sessionId);
            isFavorite = true;
            _logger.LogDebug("Added session {SessionId} to favorites", sessionId);
        }

        await SaveFavoritesAsync();
        FavoritesChanged?.Invoke(this, sessionId);
        
        return isFavorite;
    }

    public async Task AddFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoriteSessionIdsAsync();
        
        if (favorites.Add(sessionId))
        {
            await SaveFavoritesAsync();
            FavoritesChanged?.Invoke(this, sessionId);
            _logger.LogDebug("Added session {SessionId} to favorites", sessionId);
        }
    }

    public async Task RemoveFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoriteSessionIdsAsync();
        
        if (favorites.Remove(sessionId))
        {
            await SaveFavoritesAsync();
            FavoritesChanged?.Invoke(this, sessionId);
            _logger.LogDebug("Removed session {SessionId} from favorites", sessionId);
        }
    }

    private async Task SaveFavoritesAsync()
    {
        if (_favorites == null) return;

        try
        {
            await _cache.InsertObject(FavoritesCacheKey, _favorites);
            _logger.LogDebug("Saved {Count} favorites to cache", _favorites.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving favorites");
        }
    }
}
