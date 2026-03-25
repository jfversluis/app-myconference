using System.Reactive.Linq;
using Akavache;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;

namespace Conference.Maui.Services;

public class FavoritesService : IFavoritesService
{
    private readonly IBlobCache _cache;
    private readonly ILogger<FavoritesService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HashSet<string>? _favorites;
    private const string FavoritesCacheKey = "user_favorites";

    public FavoritesService(ILogger<FavoritesService> logger)
    {
        _cache = BlobCache.UserAccount;
        _logger = logger;
    }

    /// <summary>
    /// Loads favorites from cache if not yet loaded. Must be called within the lock.
    /// </summary>
    private async Task EnsureFavoritesLoadedAsync()
    {
        if (_favorites != null) return;

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
    }

    public async Task<IReadOnlySet<string>> GetFavoriteSessionIdsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureFavoritesLoadedAsync();
            return _favorites!.ToHashSet();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsFavoriteAsync(string sessionId)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureFavoritesLoadedAsync();
            return _favorites!.Contains(sessionId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ToggleFavoriteAsync(string sessionId)
    {
        bool isFavorite;
        await _lock.WaitAsync();
        try
        {
            await EnsureFavoritesLoadedAsync();

            if (_favorites!.Contains(sessionId))
            {
                _favorites.Remove(sessionId);
                isFavorite = false;
                _logger.LogDebug("Removed session {SessionId} from favorites", sessionId);
            }
            else
            {
                _favorites.Add(sessionId);
                isFavorite = true;
                _logger.LogDebug("Added session {SessionId} to favorites", sessionId);
            }

            await SaveFavoritesAsync();
        }
        finally
        {
            _lock.Release();
        }

        WeakReferenceMessenger.Default.Send(new FavoriteChangedMessage(sessionId, isFavorite));
        return isFavorite;
    }

    public async Task AddFavoriteAsync(string sessionId)
    {
        bool added;
        await _lock.WaitAsync();
        try
        {
            await EnsureFavoritesLoadedAsync();
            added = _favorites!.Add(sessionId);
            if (added)
            {
                await SaveFavoritesAsync();
                _logger.LogDebug("Added session {SessionId} to favorites", sessionId);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (added)
        {
            WeakReferenceMessenger.Default.Send(new FavoriteChangedMessage(sessionId, true));
        }
    }

    public async Task RemoveFavoriteAsync(string sessionId)
    {
        bool removed;
        await _lock.WaitAsync();
        try
        {
            await EnsureFavoritesLoadedAsync();
            removed = _favorites!.Remove(sessionId);
            if (removed)
            {
                await SaveFavoritesAsync();
                _logger.LogDebug("Removed session {SessionId} from favorites", sessionId);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (removed)
        {
            WeakReferenceMessenger.Default.Send(new FavoriteChangedMessage(sessionId, false));
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
