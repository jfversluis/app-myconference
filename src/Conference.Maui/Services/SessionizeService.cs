using System.Linq;
using System.Net.Http.Json;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class SessionizeService : IEventDataService
{
    private List<Speaker> _speakers = [];
    private List<Session> _sessions = [];

    private readonly HttpClient _httpClient = new();

    private async Task GetAllData()
    {
        var remoteAllData = await _httpClient.GetFromJsonAsync<AllData>(
            $"https://sessionize.com/api/v2/jl4ktls0/view/All");

        _speakers = remoteAllData?.Speakers?.Select(speaker => new Speaker
        {
            Id = speaker.Id,
            FirstName = speaker.FirstName ?? string.Empty,
            LastName = speaker.LastName ?? string.Empty,
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
            Room = remoteAllData.Rooms?.FirstOrDefault(room => session.RoomId == room.Id)?.Name ?? string.Empty,
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
    }

    public async Task<List<Speaker>> GetAllSpeakers()
    {
        // TODO add way to refresh data/hard refresh
        if (_speakers.Count == 0)
        {
            await GetAllData();
        }

        return _speakers;
    }

    public async Task<List<Session>> GetAllSessions()
    {
        // TODO add way to refresh data/hard refresh
        if (_sessions.Count == 0)
        {
            await GetAllData();
        }

        return _sessions;
    }
}
