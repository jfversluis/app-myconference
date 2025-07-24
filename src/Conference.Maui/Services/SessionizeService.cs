using System.Net.Http.Json;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class SessionizeService : IEventDataService
{
    private List<Speaker> _speakers = [];
    private List<Session> _sessions = [];
    private List<Room> _rooms = [];

    private readonly HttpClient _httpClient = new();
    private readonly IDatabaseService _databaseService;

    private const string EventId = "5g27052o";

    public SessionizeService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<string?> GetDataHashAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://sessionize.com/api/v2/{EventId}/view/All?hashOnly=true");
            return response?.Trim('"'); // Remove quotes from hash response
        }
        catch
        {
            return null;
        }
    }

    public async Task<(List<Session> sessions, List<Speaker> speakers, List<Room> rooms)> GetAllDataAsync()
    {
        var remoteAllData = await _httpClient.GetFromJsonAsync<AllData>(
            $"https://sessionize.com/api/v2/{EventId}/view/All");

        var speakers = remoteAllData?.Speakers?.Select(speaker => new Speaker
        {
            Id = speaker.Id,
            FirstName = speaker.FirstName ?? string.Empty,
            LastName = speaker.LastName ?? string.Empty,
            FullName = speaker.FullName ?? string.Empty,
            ProfilePicture = speaker.ProfilePicture ?? string.Empty,
            TagLine = speaker.TagLine ?? string.Empty,
            Bio = speaker.Bio ?? string.Empty,
            Links = speaker.Links?.Select(link => new Link
            {
                Title = link.Title ?? string.Empty,
                Url = link.Url ?? string.Empty,
                LinkType = link.LinkType ?? string.Empty
            }).ToList() ?? [],
            SessionIds = speaker.SessionIds,
        }).ToList() ?? [];

        var rooms = remoteAllData?.Rooms?.ToList() ?? [];

        var sessions = remoteAllData?.Sessions?.Select(session => new Session
        {
            Description = session.Description ?? string.Empty,
            EndsAt = session.EndsAt,
            Id = session.Id,
            IsConfirmed = session.IsConfirmed,
            IsInformed = session.IsInformed,
            IsPlenumSession = session.IsPlenumSession,
            IsServiceSession = session.IsServiceSession,
            RoomId = session.RoomId,
            Room = rooms.FirstOrDefault(room => session.RoomId == room.Id)?.Name ?? string.Empty,
            RoomObject = rooms.FirstOrDefault(room => session.RoomId == room.Id),
            SpeakerIds = session.SpeakerIds,
            Speakers = speakers.Where(s => session.SpeakerIds.Contains(s.Id)).ToList(),
            StartsAt = session.StartsAt,
            Status = session.Status,
            Title = session.Title,
        }).ToList() ?? [];

        // Update speakers' sessions references
        foreach (var speaker in speakers)
        {
            speaker.Sessions = sessions.Where(session => session.SpeakerIds.Contains(speaker.Id)).ToList();
        }

        return (sessions, speakers, rooms);
    }

    public async Task<RefreshResult> RefreshDataAsync()
    {
        try
        {
            // Get current hash from remote
            var remoteHash = await GetDataHashAsync();
            if (string.IsNullOrEmpty(remoteHash))
            {
                return RefreshResult.Failed; // Can't verify, treat as failed
            }

            // Get stored hash
            var localHash = await _databaseService.GetDataHashAsync();

            // Only fetch if hashes are different or no local hash exists
            if (localHash != remoteHash)
            {
                var (sessions, speakers, rooms) = await GetAllDataAsync();
                
                // Save to database
                await _databaseService.SaveEventDataAsync(sessions, speakers, rooms);
                await _databaseService.SaveDataHashAsync(remoteHash);

                // Update in-memory cache
                _sessions = sessions;
                _speakers = speakers;
                _rooms = rooms;

                // Update favorite status for sessions
                await UpdateFavoriteStatus(_sessions);
                
                return RefreshResult.DataUpdated;
            }
            else
            {
                return RefreshResult.NoUpdateNeeded;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we'll use cached data
            System.Diagnostics.Debug.WriteLine($"Failed to refresh data: {ex.Message}");
            return RefreshResult.Failed;
        }
    }

    private async Task EnsureDataLoaded()
    {
        if (_sessions.Count == 0)
        {
            // Try to load from database first
            if (await _databaseService.HasLocalDataAsync())
            {
                _sessions = await _databaseService.GetAllSessionsAsync();
                _speakers = await _databaseService.GetAllSpeakersAsync();
                _rooms = await _databaseService.GetAllRoomsAsync();

                // Update favorite status for sessions
                await UpdateFavoriteStatus(_sessions);
            }
            else
            {
                // No local data, fetch from remote
                var (sessions, speakers, rooms) = await GetAllDataAsync();
                _sessions = sessions;
                _speakers = speakers;
                _rooms = rooms;

                // Save to database
                var remoteHash = await GetDataHashAsync();
                await _databaseService.SaveEventDataAsync(sessions, speakers, rooms);
                if (!string.IsNullOrEmpty(remoteHash))
                {
                    await _databaseService.SaveDataHashAsync(remoteHash);
                }

                // Update favorite status for sessions
                await UpdateFavoriteStatus(_sessions);
            }
        }
    }

    private async Task UpdateFavoriteStatus(List<Session> sessions)
    {
        var favorites = await _databaseService.GetAllFavoriteSessionsAsync();
        var favoriteIds = favorites.Where(f => f.IsFavorite).Select(f => f.SessionId).ToHashSet();

        foreach (var session in sessions)
        {
            session.IsFavorite = favoriteIds.Contains(session.Id);
        }
    }

    public async Task<List<Speaker>> GetAllSpeakers()
    {
        await EnsureDataLoaded();
        return _speakers;
    }

    public async Task<List<Session>> GetAllSessions()
    {
        await EnsureDataLoaded();
        return _sessions;
    }
}
