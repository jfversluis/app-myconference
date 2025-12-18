using Akavache;
using System.Reactive.Linq;

namespace Conference.Maui.Services;

public class FavoritesService : IFavoritesService
{
    private const string FavoritesKey = "session_favorites";
    private HashSet<string> _favorites = new();
    private bool _isLoaded;

    public async Task<HashSet<string>> GetFavoritesAsync()
    {
        if (!_isLoaded)
        {
            try
            {
                _favorites = await BlobCache.UserAccount.GetObject<HashSet<string>>(FavoritesKey)
                    .Catch(Observable.Return(new HashSet<string>()));
                _isLoaded = true;
            }
            catch
            {
                _favorites = new HashSet<string>();
                _isLoaded = true;
            }
        }
        return _favorites;
    }

    public async Task AddFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoritesAsync();
        if (favorites.Add(sessionId))
        {
            await SaveFavoritesAsync();
        }
    }

    public async Task RemoveFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoritesAsync();
        if (favorites.Remove(sessionId))
        {
            await SaveFavoritesAsync();
        }
    }

    public async Task<bool> IsFavoriteAsync(string sessionId)
    {
        var favorites = await GetFavoritesAsync();
        return favorites.Contains(sessionId);
    }

    private async Task SaveFavoritesAsync()
    {
        await BlobCache.UserAccount.InsertObject(FavoritesKey, _favorites);
    }
}
