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
    private bool _isRefreshing = false;
    
    private const string API_BASE_URL = "https://sessionize.com/api/v2/5g27052o";
    private const string CACHE_KEY = "event_data";

    public event EventHandler<EventArgs>? DataRefreshed;
    public event EventHandler<bool>? RefreshStateChanged;

    public SessionizeService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<bool> IsRefreshingAsync()
    {
        return _isRefreshing;
    }

    public async Task<DateTime?> GetLastRefreshTimeAsync()
    {
        var cacheInfo = await _databaseService.GetDataCacheInfoAsync(CACHE_KEY);
        return cacheInfo?.LastChecked;
    }

    private void SetRefreshingState(bool isRefreshing)
    {
        if (_isRefreshing != isRefreshing)
        {
            _isRefreshing = isRefreshing;
            RefreshStateChanged?.Invoke(this, isRefreshing);
        }
    }

    private async Task<string> GetRemoteHashAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{API_BASE_URL}/view/All?hashOnly=true");
            return response.Trim().Trim('"');
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<bool> ShouldRefreshDataAsync(bool forceRefresh)
    {
        if (forceRefresh)
            return true;

        var cacheInfo = await _databaseService.GetDataCacheInfoAsync(CACHE_KEY);
        if (cacheInfo == null)
            return true;

        var remoteHash = await GetRemoteHashAsync();
        if (string.IsNullOrEmpty(remoteHash))
            return false;

        // Always update last checked time, even if hash hasn't changed
        cacheInfo.LastChecked = DateTime.Now;
        await _databaseService.SaveDataCacheInfoAsync(cacheInfo);

        return cacheInfo.Hash != remoteHash;
    }

    private async Task GetAllDataFromRemote()
    {
        try
        {
            var remoteAllData = await _httpClient.GetFromJsonAsync<AllData>(
                $"{API_BASE_URL}/view/All");

            if (remoteAllData == null)
            {
                System.Diagnostics.Debug.WriteLine("Failed to retrieve data from remote API");
                return;
            }

            // Store rooms first
            _rooms = remoteAllData.Rooms ?? [];

            _speakers = remoteAllData?.Speakers?.Select(speaker => new Speaker
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

            _sessions = remoteAllData?.Sessions?.Select(session => new Session
            {
                Description = session.Description ?? string.Empty,
                EndsAt = session.EndsAt,
                Id = session.Id,
                IsConfirmed = session.IsConfirmed,
                IsInformed = session.IsInformed,
                IsPlenumSession = session.IsPlenumSession,
                IsServiceSession = session.IsServiceSession,
                RoomId = session.RoomId,
                Room = _rooms.FirstOrDefault(room => session.RoomId == room.Id)?.Name ?? string.Empty,
                RoomObject = _rooms.FirstOrDefault(room => session.RoomId == room.Id),
                SpeakerIds = session.SpeakerIds,
                Speakers = _speakers.Where(s => session.SpeakerIds.Contains(s.Id)).ToList(),
                StartsAt = session.StartsAt,
                Status = session.Status,
                Title = session.Title,
            }).ToList() ?? [];

            foreach (var speaker in _speakers)
            {
                speaker.Sessions = _sessions.Where(session => session.SpeakerIds.Contains(speaker.Id)).ToList();
            }

            // Cache the data
            await CacheDataAsync();

            // Update cache info with new hash
            var remoteHash = await GetRemoteHashAsync();
            var cacheInfo = new DataCacheInfo
            {
                Key = CACHE_KEY,
                Hash = remoteHash,
                LastUpdated = DateTime.Now,
                LastChecked = DateTime.Now,
                IsRefreshing = false
            };
            await _databaseService.SaveDataCacheInfoAsync(cacheInfo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data from remote: {ex.Message}");
            // Don't rethrow - let the app continue with cached data if available
        }
    }

    private async Task CacheDataAsync()
    {
        try
        {
            // Convert and cache sessions
            var cachedSessions = _sessions.Select(CachedSession.FromSession).ToList();
            await _databaseService.SaveCachedSessionsAsync(cachedSessions);

            // Convert and cache speakers
            var cachedSpeakers = _speakers.Select(CachedSpeaker.FromSpeaker).ToList();
            await _databaseService.SaveCachedSpeakersAsync(cachedSpeakers);

            // Convert and cache rooms
            var cachedRooms = _rooms.Select(CachedRoom.FromRoom).ToList();
            await _databaseService.SaveCachedRoomsAsync(cachedRooms);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error caching data: {ex.Message}");
            // Don't rethrow - caching failure shouldn't break the app
        }
    }

    public async Task<bool> HasCachedDataAsync()
    {
        return await _databaseService.HasCachedDataAsync();
    }

    public async Task<List<Session>> GetCachedSessionsAsync()
    {
        try
        {
            var cachedSessions = await _databaseService.GetAllCachedSessionsAsync();
            var cachedSpeakers = await _databaseService.GetAllCachedSpeakersAsync();
            var cachedRooms = await _databaseService.GetAllCachedRoomsAsync();

            // Convert cached data back to models
            var speakers = cachedSpeakers.Select(cs => cs.ToSpeaker()).ToList();
            var rooms = cachedRooms.Select(cr => cr.ToRoom()).ToList();
            var sessions = cachedSessions.Select(cs =>
            {
                var session = cs.ToSession();
                session.RoomObject = rooms.FirstOrDefault(r => r.Id == session.RoomId);
                session.Speakers = speakers.Where(s => session.SpeakerIds.Contains(s.Id)).ToList();
                return session;
            }).ToList();

            // Update speaker sessions
            foreach (var speaker in speakers)
            {
                speaker.Sessions = sessions.Where(s => s.SpeakerIds.Contains(speaker.Id)).ToList();
            }

            return sessions;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error retrieving cached sessions: {ex.Message}");
            return [];
        }
    }

    public async Task<List<Speaker>> GetCachedSpeakersAsync()
    {
        try
        {
            var cachedSpeakers = await _databaseService.GetAllCachedSpeakersAsync();
            var cachedSessions = await _databaseService.GetAllCachedSessionsAsync();

            var speakers = cachedSpeakers.Select(cs => cs.ToSpeaker()).ToList();
            var sessions = cachedSessions.Select(cs => cs.ToSession()).ToList();

            // Update speaker sessions
            foreach (var speaker in speakers)
            {
                speaker.Sessions = sessions.Where(s => s.SpeakerIds.Contains(speaker.Id)).ToList();
            }

            return speakers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error retrieving cached speakers: {ex.Message}");
            return [];
        }
    }

    public async Task RefreshDataAsync(bool forceRefresh = false)
    {
        if (_isRefreshing)
            return;

        try
        {
            SetRefreshingState(true);

            if (await ShouldRefreshDataAsync(forceRefresh))
            {
                await GetAllDataFromRemote();
                DataRefreshed?.Invoke(this, EventArgs.Empty);
            }
            else if (forceRefresh)
            {
                // Even when hash doesn't change during manual refresh, 
                // we should update the LastChecked time and notify UI
                var cacheInfo = await _databaseService.GetDataCacheInfoAsync(CACHE_KEY);
                if (cacheInfo != null)
                {
                    cacheInfo.LastChecked = DateTime.Now;
                    await _databaseService.SaveDataCacheInfoAsync(cacheInfo);
                }
                DataRefreshed?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            SetRefreshingState(false);
        }
    }

    public async Task<List<Speaker>> GetAllSpeakers()
    {
        // If we have cached data and haven't loaded it yet, load from cache first
        if (_speakers.Count == 0 && await HasCachedDataAsync())
        {
            _speakers = await GetCachedSpeakersAsync();
        }

        // If still no data, refresh from remote
        if (_speakers.Count == 0)
        {
            await RefreshDataAsync(forceRefresh: true);
        }

        return _speakers;
    }

    public async Task<List<Session>> GetAllSessions()
    {
        // If we have cached data and haven't loaded it yet, load from cache first
        if (_sessions.Count == 0 && await HasCachedDataAsync())
        {
            _sessions = await GetCachedSessionsAsync();
        }

        // If still no data, refresh from remote
        if (_sessions.Count == 0)
        {
            await RefreshDataAsync(forceRefresh: true);
        }

        return _sessions;
    }
}
