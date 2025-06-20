using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class CachedEventDataService : IEventDataService
{
    private readonly SessionizeService _remoteService;
    private readonly IDatabaseService _databaseService;
    private readonly TimeSpan _cacheMaxAge = TimeSpan.FromHours(1); // Cache expires after 1 hour

    public CachedEventDataService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _remoteService = new SessionizeService();
    }

    public async Task<List<Session>> GetAllSessions()
    {
        await _databaseService.InitializeAsync();

        // Check if cache is expired or empty
        if (await _databaseService.IsCacheExpiredAsync(_cacheMaxAge))
        {
            await RefreshDataFromRemote();
        }

        // Get sessions from cache
        var cachedSessions = await _databaseService.GetCachedSessionsAsync();
        var sessions = cachedSessions.Select(cs => cs.ToSession()).ToList();

        // Merge with favorites and populate speakers
        await EnrichSessionsWithFavoritesAndSpeakers(sessions);

        return sessions;
    }

    public async Task<List<Speaker>> GetAllSpeakers()
    {
        await _databaseService.InitializeAsync();

        // Check if cache is expired or empty
        if (await _databaseService.IsCacheExpiredAsync(_cacheMaxAge))
        {
            await RefreshDataFromRemote();
        }

        // Get speakers from cache
        var cachedSpeakers = await _databaseService.GetCachedSpeakersAsync();
        var speakers = cachedSpeakers.Select(cs => cs.ToSpeaker()).ToList();

        // Populate sessions for each speaker
        await PopulateSpeakerSessions(speakers);

        return speakers;
    }

    public async Task RefreshDataAsync()
    {
        await RefreshDataFromRemote();
    }

    private async Task RefreshDataFromRemote()
    {
        try
        {
            // Get fresh data from remote service
            var remoteSessions = await _remoteService.GetAllSessions();
            var remoteSpeakers = await _remoteService.GetAllSpeakers();

            // Convert to cached models
            var cachedSessions = remoteSessions.Select(CachedSession.FromSession).ToList();
            var cachedSpeakers = remoteSpeakers.Select(CachedSpeaker.FromSpeaker).ToList();

            // For rooms, we need to extract them from sessions
            var rooms = remoteSessions
                .Where(s => s.RoomId > 0)
                .GroupBy(s => s.RoomId)
                .Select(g => new Room 
                { 
                    Id = g.Key, 
                    Name = g.First().Room 
                })
                .ToList();
            var cachedRooms = rooms.Select(CachedRoom.FromRoom).ToList();

            // Save to cache
            await _databaseService.SaveCachedSessionsAsync(cachedSessions);
            await _databaseService.SaveCachedSpeakersAsync(cachedSpeakers);
            await _databaseService.SaveCachedRoomsAsync(cachedRooms);

            // Update cache timestamp
            await _databaseService.SetLastCacheUpdateAsync(DateTime.UtcNow);
        }
        catch (Exception)
        {
            // If remote fetch fails, we'll use cached data
            // In a production app, you might want to log this error
        }
    }

    private async Task EnrichSessionsWithFavoritesAndSpeakers(List<Session> sessions)
    {
        // Get all favorites
        var favorites = await _databaseService.GetAllFavoriteSessionsAsync();
        var favoriteSessionIds = favorites.Where(f => f.IsFavorite).Select(f => f.SessionId).ToHashSet();

        // Get all speakers
        var cachedSpeakers = await _databaseService.GetCachedSpeakersAsync();
        var speakerLookup = cachedSpeakers.ToDictionary(s => s.Id, s => s.ToSpeaker());

        // Enrich sessions
        foreach (var session in sessions)
        {
            // Add favorite status (this could be added as an extension property if needed)
            // For now, the favorite status is managed separately through IDatabaseService

            // Populate speakers
            session.Speakers = session.SpeakerIds
                .Where(id => speakerLookup.ContainsKey(id))
                .Select(id => speakerLookup[id])
                .ToList();
        }
    }

    private async Task PopulateSpeakerSessions(List<Speaker> speakers)
    {
        // Get all sessions
        var cachedSessions = await _databaseService.GetCachedSessionsAsync();
        var sessionLookup = cachedSessions.ToDictionary(s => s.Id, s => s.ToSession());

        // Populate sessions for each speaker
        foreach (var speaker in speakers)
        {
            // Convert SessionIds from int to string for lookup
            var sessionIds = speaker.SessionIds.Select(id => id.ToString()).ToList();
            speaker.Sessions = sessionIds
                .Where(id => sessionLookup.ContainsKey(id))
                .Select(id => sessionLookup[id])
                .ToList();
        }
    }
}