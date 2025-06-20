using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using SQLite;

namespace Conference.Maui.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection _database;
    private bool _isInitialized = false;
    private readonly string _databasePath;

    public DatabaseService()
    {
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "ConferenceApp.db3");
    }

    async Task InitializeDatabaseAsync()
    {
        if (_isInitialized)
            return;

        _database = new SQLiteAsyncConnection(_databasePath);
        await _database.CreateTableAsync<FavoriteSession>();
        await _database.CreateTableAsync<CachedSession>();
        await _database.CreateTableAsync<CachedSpeaker>();
        await _database.CreateTableAsync<CachedRoom>();
        await _database.CreateTableAsync<CacheMetadata>();
        
        _isInitialized = true;
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    #region Favorite Sessions

    public async Task<List<FavoriteSession>> GetAllFavoriteSessionsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<FavoriteSession>().ToListAsync();
    }

    public async Task<FavoriteSession> GetFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        return await _database.Table<FavoriteSession>()
            .Where(f => f.SessionId == sessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveFavoriteSessionAsync(FavoriteSession favoriteSession)
    {
        await InitializeDatabaseAsync();
        
        if (await GetFavoriteSessionAsync(favoriteSession.SessionId) != null)
        {
            return await _database.UpdateAsync(favoriteSession) > 0;
        }
        else
        {
            return await _database.InsertAsync(favoriteSession) > 0;
        }
    }

    public async Task<bool> DeleteFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        return await _database.DeleteAsync<FavoriteSession>(sessionId) > 0;
    }

    public async Task<bool> IsFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        var favoriteSession = await GetFavoriteSessionAsync(sessionId);
        return favoriteSession != null && favoriteSession.IsFavorite;
    }

    #endregion

    #region Cached Event Data

    public async Task<List<CachedSession>> GetCachedSessionsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedSession>().ToListAsync();
    }

    public async Task<List<CachedSpeaker>> GetCachedSpeakersAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedSpeaker>().ToListAsync();
    }

    public async Task<List<CachedRoom>> GetCachedRoomsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedRoom>().ToListAsync();
    }

    public async Task SaveCachedSessionsAsync(List<CachedSession> sessions)
    {
        await InitializeDatabaseAsync();
        await _database.DeleteAllAsync<CachedSession>();
        if (sessions.Any())
        {
            await _database.InsertAllAsync(sessions);
        }
    }

    public async Task SaveCachedSpeakersAsync(List<CachedSpeaker> speakers)
    {
        await InitializeDatabaseAsync();
        await _database.DeleteAllAsync<CachedSpeaker>();
        if (speakers.Any())
        {
            await _database.InsertAllAsync(speakers);
        }
    }

    public async Task SaveCachedRoomsAsync(List<CachedRoom> rooms)
    {
        await InitializeDatabaseAsync();
        await _database.DeleteAllAsync<CachedRoom>();
        if (rooms.Any())
        {
            await _database.InsertAllAsync(rooms);
        }
    }

    public async Task ClearCachedDataAsync()
    {
        await InitializeDatabaseAsync();
        await _database.DeleteAllAsync<CachedSession>();
        await _database.DeleteAllAsync<CachedSpeaker>();
        await _database.DeleteAllAsync<CachedRoom>();
    }

    #endregion

    #region Cache Metadata

    public async Task<DateTime?> GetLastCacheUpdateAsync()
    {
        await InitializeDatabaseAsync();
        var metadata = await _database.Table<CacheMetadata>()
            .Where(m => m.Key == "LastUpdate")
            .FirstOrDefaultAsync();
        return metadata?.LastUpdated;
    }

    public async Task SetLastCacheUpdateAsync(DateTime timestamp)
    {
        await InitializeDatabaseAsync();
        var metadata = new CacheMetadata
        {
            Key = "LastUpdate",
            LastUpdated = timestamp,
            Version = "1.0"
        };
        
        var existing = await _database.Table<CacheMetadata>()
            .Where(m => m.Key == "LastUpdate")
            .FirstOrDefaultAsync();
            
        if (existing != null)
        {
            metadata.Key = existing.Key;
            await _database.UpdateAsync(metadata);
        }
        else
        {
            await _database.InsertAsync(metadata);
        }
    }

    public async Task<bool> IsCacheExpiredAsync(TimeSpan maxAge)
    {
        var lastUpdate = await GetLastCacheUpdateAsync();
        if (!lastUpdate.HasValue)
            return true;
            
        return DateTime.UtcNow - lastUpdate.Value > maxAge;
    }

    #endregion
}
