using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sessionize.Api.Client;
using Sessionize.Api.Client.Configuration;

namespace Conference.Maui.Services;

public class SessionizeService : IEventDataService
{
    private readonly SessionizeApiClient _sessionizeClient;

    private List<Speaker> _speakers = [];
    private List<Session> _sessions = [];

    public SessionizeService(IHttpClientFactory httpClientFactory, ILogger<SessionizeApiClient> logger)
    {
        _sessionizeClient = new SessionizeApiClient(httpClientFactory, logger, Options.Create(new SessionizeConfiguration
        {
            ApiId = "8cknf5zh",
            BaseUrl = "https://sessionize.com"
        }));
    }

    private async Task GetAllData()
    {
        var remoteAllData = await _sessionizeClient.GetAllDataAsync();

        _speakers = remoteAllData.Speakers.Select(speaker => new Speaker
        {
            Bio = speaker.Bio,
            FirstName = speaker.FirstName,
            Id = Guid.Parse(speaker.Id),
            IsTopSpeaker = speaker.IsTopSpeaker,
            LastName = speaker.LastName,
            ProfilePictureUrl = speaker.ProfilePicture,
            TagLine = speaker.TagLine,
        }).ToList() ?? [];

        _sessions = remoteAllData?.Sessions.Select(session => new Session
        {
            Description = session.Description ?? string.Empty,
            EndsAt = session.EndsAt,
            Id = session.Id,
            IsConfirmed = session.IsConfirmed,
            IsInformed = session.IsInformed,
            IsPlenumSession = session.IsPlenumSession,
            IsServiceSession = session.IsServiceSession,
            RoomId = session.RoomId,
            Room = remoteAllData.Rooms.FirstOrDefault(room => session.RoomId == room.Id)?.Name ?? string.Empty,
            Speakers = _speakers.Where(s => session.Speakers.Contains(s.Id.ToString())).ToList(),
            StartsAt = session.StartsAt,
            Status = session.Status,
            Title = session.Title,
        }).ToList() ?? [];

        foreach (var speaker in _speakers)
        {
            speaker.Sessions = _sessions.Where(session => session.Speakers.Any(s => s.Id == speaker.Id)).ToList();
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
