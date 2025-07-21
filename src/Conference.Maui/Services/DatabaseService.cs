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
        
        // Create all tables
        await _database.CreateTableAsync<FavoriteSession>();
        await _database.CreateTableAsync<CachedSession>();
        await _database.CreateTableAsync<CachedSpeaker>();
        await _database.CreateTableAsync<CachedRoom>();
        await _database.CreateTableAsync<DataCacheInfo>();
        
        _isInitialized = true;
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    // Favorite sessions implementation
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

    // Cached event data implementation
    public async Task<List<CachedSession>> GetAllCachedSessionsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedSession>().ToListAsync();
    }

    public async Task<List<CachedSpeaker>> GetAllCachedSpeakersAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedSpeaker>().ToListAsync();
    }

    public async Task<List<CachedRoom>> GetAllCachedRoomsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database.Table<CachedRoom>().ToListAsync();
    }

    public async Task SaveCachedSessionsAsync(List<CachedSession> sessions)
    {
        await InitializeDatabaseAsync();
        
        // Clear existing sessions first
        await _database.DeleteAllAsync<CachedSession>();
        
        // Insert new sessions
        if (sessions.Any())
        {
            await _database.InsertAllAsync(sessions);
        }
    }

    public async Task SaveCachedSpeakersAsync(List<CachedSpeaker> speakers)
    {
        await InitializeDatabaseAsync();
        
        // Clear existing speakers first
        await _database.DeleteAllAsync<CachedSpeaker>();
        
        // Insert new speakers
        if (speakers.Any())
        {
            await _database.InsertAllAsync(speakers);
        }
    }

    public async Task SaveCachedRoomsAsync(List<CachedRoom> rooms)
    {
        await InitializeDatabaseAsync();
        
        // Clear existing rooms first
        await _database.DeleteAllAsync<CachedRoom>();
        
        // Insert new rooms
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

    // Data cache info implementation
    public async Task<DataCacheInfo?> GetDataCacheInfoAsync(string key)
    {
        await InitializeDatabaseAsync();
        return await _database.Table<DataCacheInfo>()
            .Where(d => d.Key == key)
            .FirstOrDefaultAsync();
    }

    public async Task SaveDataCacheInfoAsync(DataCacheInfo cacheInfo)
    {
        await InitializeDatabaseAsync();
        
        var existing = await GetDataCacheInfoAsync(cacheInfo.Key);
        if (existing != null)
        {
            await _database.UpdateAsync(cacheInfo);
        }
        else
        {
            await _database.InsertAsync(cacheInfo);
        }
    }

    public async Task<bool> HasCachedDataAsync()
    {
        await InitializeDatabaseAsync();
        
        var sessionCount = await _database.Table<CachedSession>().CountAsync();
        var speakerCount = await _database.Table<CachedSpeaker>().CountAsync();
        
        return sessionCount > 0 && speakerCount > 0;
    }
}
