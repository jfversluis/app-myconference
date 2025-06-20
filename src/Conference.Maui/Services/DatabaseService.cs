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
        
        _isInitialized = true;
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

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
}
