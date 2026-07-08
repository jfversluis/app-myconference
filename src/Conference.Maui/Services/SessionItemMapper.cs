using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.Services;

/// <summary>
/// Centralizes SessionDetails → SessionItem mapping with O(1) speaker and room lookups.
/// </summary>
public class SessionItemMapper : ISessionItemMapper
{
    private readonly IEventTimeService _eventTimeService;
    private Dictionary<string, SpeakerDetails> _speakerLookup = [];
    private Dictionary<int, string> _roomLookup = [];

    public SessionItemMapper(IEventTimeService eventTimeService)
    {
        _eventTimeService = eventTimeService;
    }

    public void Initialize(AllDataResponse allData)
    {
        _speakerLookup = allData.Speakers.ToDictionary(s => s.Id);
        _roomLookup = allData.Rooms.ToDictionary(r => r.Id, r => r.Name);
    }

    public SessionItem MapSession(SessionDetails session, IReadOnlySet<string>? favoriteIds = null, IReadOnlySet<string>? reminderIds = null)
    {
        var speakers = session.Speakers
            .Select(sid => _speakerLookup.GetValueOrDefault(sid))
            .Where(s => s != null)
            .Select(s => SpeakerItem.FromSpeakerDetails(s!))
            .ToList();

        var isFavorite = favoriteIds?.Contains(session.Id) ?? false;

        return new SessionItem
        {
            Id = session.Id,
            Title = session.Title,
            Description = session.Description,
            StartsAt = _eventTimeService.NormalizeSessionizeLocalTime(session.StartsAt),
            EndsAt = _eventTimeService.NormalizeSessionizeLocalTime(session.EndsAt),
            RoomId = session.RoomId,
            RoomName = _roomLookup.GetValueOrDefault(session.RoomId),
            Speakers = speakers,
            IsFavorite = isFavorite,
            HasReminder = isFavorite && (reminderIds?.Contains(session.Id) ?? false)
        };
    }

    public List<SessionItem> MapSessions(IEnumerable<SessionDetails> sessions, IReadOnlySet<string>? favoriteIds = null, IReadOnlySet<string>? reminderIds = null)
    {
        return sessions.Select(s => MapSession(s, favoriteIds, reminderIds)).ToList();
    }

    public string? GetRoomName(int roomId)
    {
        return _roomLookup.GetValueOrDefault(roomId);
    }
}
