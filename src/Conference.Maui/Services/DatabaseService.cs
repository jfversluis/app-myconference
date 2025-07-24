using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Models.Database;
using SQLite;

namespace Conference.Maui.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private bool _isInitialized = false;
    private readonly string _databasePath;

    private const string DataHashKey = "data_hash";

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
        await _database.CreateTableAsync<SessionDb>();
        await _database.CreateTableAsync<SpeakerDb>();
        await _database.CreateTableAsync<RoomDb>();
        await _database.CreateTableAsync<DataMetadata>();
        
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
        return await _database!.Table<FavoriteSession>().ToListAsync();
    }

    public async Task<FavoriteSession> GetFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<FavoriteSession>()
            .Where(f => f.SessionId == sessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveFavoriteSessionAsync(FavoriteSession favoriteSession)
    {
        await InitializeDatabaseAsync();
        
        var existing = await GetFavoriteSessionAsync(favoriteSession.SessionId);
        if (existing != null)
        {
            return await _database!.UpdateAsync(favoriteSession) > 0;
        }
        else
        {
            return await _database!.InsertAsync(favoriteSession) > 0;
        }
    }

    public async Task<bool> DeleteFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        return await _database!.DeleteAsync<FavoriteSession>(sessionId) > 0;
    }

    public async Task<bool> IsFavoriteSessionAsync(string sessionId)
    {
        await InitializeDatabaseAsync();
        var favoriteSession = await GetFavoriteSessionAsync(sessionId);
        return favoriteSession != null && favoriteSession.IsFavorite;
    }

    #endregion

    #region Event Data

    public async Task<List<Session>> GetAllSessionsAsync()
    {
        await InitializeDatabaseAsync();
        
        var sessionDbs = await _database!.Table<SessionDb>().ToListAsync();
        var speakerDbs = await _database!.Table<SpeakerDb>().ToListAsync();
        var speakers = speakerDbs.Select(sdb => sdb.ToSpeaker()).ToList();
        var rooms = await GetAllRoomsAsync();
        
        var sessions = sessionDbs.Select(sdb => sdb.ToSession(speakers, rooms)).ToList();
        
        // Update speakers' sessions references
        foreach (var speaker in speakers)
        {
            speaker.Sessions = sessions.Where(s => s.SpeakerIds.Contains(speaker.Id)).ToList();
        }
        
        return sessions;
    }

    public async Task<List<Speaker>> GetAllSpeakersAsync()
    {
        await InitializeDatabaseAsync();
        
        var speakerDbs = await _database!.Table<SpeakerDb>().ToListAsync();
        var speakers = speakerDbs.Select(sdb => sdb.ToSpeaker()).ToList();
        
        // Get all sessions to populate speakers' sessions references
        var sessionDbs = await _database!.Table<SessionDb>().ToListAsync();
        var roomDbs = await _database!.Table<RoomDb>().ToListAsync();
        var rooms = roomDbs.Select(rdb => rdb.ToRoom()).ToList();
        var sessions = sessionDbs.Select(sdb => sdb.ToSession(speakers, rooms)).ToList();
        
        // Update speakers' sessions references
        foreach (var speaker in speakers)
        {
            speaker.Sessions = sessions.Where(s => s.SpeakerIds.Contains(speaker.Id)).ToList();
        }
        
        return speakers;
    }

    public async Task<List<Room>> GetAllRoomsAsync()
    {
        await InitializeDatabaseAsync();
        
        var roomDbs = await _database!.Table<RoomDb>().ToListAsync();
        return roomDbs.Select(rdb => rdb.ToRoom()).ToList();
    }

    public async Task SaveEventDataAsync(List<Session> sessions, List<Speaker> speakers, List<Room> rooms)
    {
        await InitializeDatabaseAsync();

        // Get existing favorite sessions to preserve them
        var existingFavorites = await GetAllFavoriteSessionsAsync();

        // Clear existing event data
        await _database!.DeleteAllAsync<SessionDb>();
        await _database!.DeleteAllAsync<SpeakerDb>();
        await _database!.DeleteAllAsync<RoomDb>();

        // Insert new data
        var sessionDbs = sessions.Select(SessionDb.FromSession).ToList();
        var speakerDbs = speakers.Select(SpeakerDb.FromSpeaker).ToList();
        var roomDbs = rooms.Select(RoomDb.FromRoom).ToList();

        await _database!.InsertAllAsync(sessionDbs);
        await _database!.InsertAllAsync(speakerDbs);
        await _database!.InsertAllAsync(roomDbs);

        // Restore favorite sessions (they are preserved automatically since we don't clear that table)
        // But we should ensure they still exist in the new data
        var validFavorites = existingFavorites.Where(f => sessions.Any(s => s.Id == f.SessionId)).ToList();
        var invalidFavorites = existingFavorites.Where(f => !sessions.Any(s => s.Id == f.SessionId)).ToList();

        // Remove favorites for sessions that no longer exist
        foreach (var invalidFavorite in invalidFavorites)
        {
            await DeleteFavoriteSessionAsync(invalidFavorite.SessionId);
        }
    }

    public async Task<bool> HasLocalDataAsync()
    {
        await InitializeDatabaseAsync();
        
        var sessionCount = await _database!.Table<SessionDb>().CountAsync();
        return sessionCount > 0;
    }

    public async Task ClearEventDataAsync()
    {
        await InitializeDatabaseAsync();
        
        await _database!.DeleteAllAsync<SessionDb>();
        await _database!.DeleteAllAsync<SpeakerDb>();
        await _database!.DeleteAllAsync<RoomDb>();
        
        // Clear the hash as well
        await _database!.DeleteAsync<DataMetadata>(DataHashKey);
    }

    #endregion

    #region Data Hash Management

    public async Task<string?> GetDataHashAsync()
    {
        await InitializeDatabaseAsync();
        
        var metadata = await _database!.Table<DataMetadata>()
            .Where(m => m.Key == DataHashKey)
            .FirstOrDefaultAsync();
            
        return metadata?.Value;
    }

    public async Task SaveDataHashAsync(string hash)
    {
        await InitializeDatabaseAsync();
        
        var existing = await _database!.Table<DataMetadata>()
            .Where(m => m.Key == DataHashKey)
            .FirstOrDefaultAsync();

        var metadata = new DataMetadata
        {
            Key = DataHashKey,
            Value = hash,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow
        };

        if (existing != null)
        {
            await _database!.UpdateAsync(metadata);
        }
        else
        {
            await _database!.InsertAsync(metadata);
        }
    }

    #endregion
}
