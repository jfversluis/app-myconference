﻿using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sessionize.Api.Client;
using Sessionize.Api.Client.Configuration;

namespace Conference.Maui.Services;

public class SessionizeService : IEventDataService
{
    private readonly SessionizeApiClient _sessionizeClient;

    public SessionizeService(IHttpClientFactory httpClientFactory, ILogger<SessionizeApiClient> logger)
    {
        _sessionizeClient = new SessionizeApiClient(httpClientFactory, logger, Options.Create(new SessionizeConfiguration
        {
            ApiId = "8cknf5zh",
            BaseUrl = "https://sessionize.com"
        }));
    }

    public async Task<List<Speaker>> GetAllSpeakers()
    {
        var remoteSpeakers = await _sessionizeClient.GetSpeakersListAsync();

        return remoteSpeakers.Select(speaker => new Speaker
        {
            Bio = speaker.Bio,
            FirstName = speaker.FirstName,
            Id = speaker.Id,
            IsTopSpeaker = speaker.IsTopSpeaker,
            LastName = speaker.LastName,
            ProfilePictureUrl = speaker.ProfilePicture,
            TagLine = speaker.TagLine,
            //Sessions = speaker.Sessions,
        }).ToList();
    }

    public async Task<List<Session>> GetAllSessions()
    {
        // TODO what are groups in Sessionize?
        var remoteSessions = await _sessionizeClient.GetSessionsListAsync();

        return remoteSessions.FirstOrDefault()?.Sessions.Select(session => new Session
        {
            Description = session.Description ?? string.Empty,
            EndsAt = session.EndsAt,
            Id = session.Id,
            IsConfirmed = session.IsConfirmed,
            IsInformed = session.IsInformed,
            IsPlenumSession = session.IsPlenumSession,
            IsServiceSession = session.IsServiceSession,
            RoomId = session.RoomId,
            Room = session.Room,
            Speakers = session.Speakers.Select(speaker => new Speaker
            {
                // TODO first only get minimal info?
                Id = Guid.Parse(speaker.Id),
                FirstName = speaker.Name,
            }).ToList(),
            StartsAt = session.StartsAt,
            Status = session.Status,
            Title = session.Title,
        }).ToList() ?? [];
    }
}
