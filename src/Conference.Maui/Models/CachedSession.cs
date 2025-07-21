using SQLite;
using System.Text.Json;

namespace Conference.Maui.Models;

[Table("CachedSessions")]
public class CachedSession
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsServiceSession { get; set; }

    public bool IsPlenumSession { get; set; }

    public string SpeakerIds { get; set; } = string.Empty; // JSON serialized list

    public int RoomId { get; set; }

    public string Room { get; set; } = string.Empty;

    public string LiveUrl { get; set; } = string.Empty;

    public string RecordingUrl { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsInformed { get; set; }

    public bool IsConfirmed { get; set; }

    public DateTime CachedAt { get; set; }

    // Convert to Session model
    public Session ToSession()
    {
        return new Session
        {
            Id = Id,
            Title = Title,
            Description = Description,
            StartsAt = StartsAt,
            EndsAt = EndsAt,
            IsServiceSession = IsServiceSession,
            IsPlenumSession = IsPlenumSession,
            SpeakerIds = string.IsNullOrEmpty(SpeakerIds) ? [] : JsonSerializer.Deserialize<List<string>>(SpeakerIds) ?? [],
            RoomId = RoomId,
            Room = Room,
            LiveUrl = LiveUrl,
            RecordingUrl = RecordingUrl,
            Status = Status,
            IsInformed = IsInformed,
            IsConfirmed = IsConfirmed
        };
    }

    // Create from Session model
    public static CachedSession FromSession(Session session)
    {
        return new CachedSession
        {
            Id = session.Id,
            Title = session.Title,
            Description = session.Description,
            StartsAt = session.StartsAt,
            EndsAt = session.EndsAt,
            IsServiceSession = session.IsServiceSession,
            IsPlenumSession = session.IsPlenumSession,
            SpeakerIds = JsonSerializer.Serialize(session.SpeakerIds),
            RoomId = session.RoomId,
            Room = session.Room,
            LiveUrl = session.LiveUrl,
            RecordingUrl = session.RecordingUrl,
            Status = session.Status,
            IsInformed = session.IsInformed,
            IsConfirmed = session.IsConfirmed,
            CachedAt = DateTime.Now
        };
    }
}