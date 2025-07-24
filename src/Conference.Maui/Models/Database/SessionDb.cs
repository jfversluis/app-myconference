using SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conference.Maui.Models.Database;

[Table("Sessions")]
public class SessionDb
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

    public DateTime LastUpdated { get; set; }

    // Convert to Session model
    public Session ToSession(List<Speaker> speakers, List<Room> rooms)
    {
        var speakerIdList = string.IsNullOrEmpty(SpeakerIds) ? 
            new List<string>() : 
            JsonSerializer.Deserialize<List<string>>(SpeakerIds) ?? new List<string>();

        var roomObject = rooms.FirstOrDefault(r => r.Id == RoomId);

        return new Session
        {
            Id = Id,
            Title = Title,
            Description = Description,
            StartsAt = StartsAt,
            EndsAt = EndsAt,
            IsServiceSession = IsServiceSession,
            IsPlenumSession = IsPlenumSession,
            SpeakerIds = speakerIdList,
            Speakers = speakers.Where(s => speakerIdList.Contains(s.Id)).ToList(),
            RoomId = RoomId,
            Room = Room,
            RoomObject = roomObject,
            LiveUrl = LiveUrl,
            RecordingUrl = RecordingUrl,
            Status = Status,
            IsInformed = IsInformed,
            IsConfirmed = IsConfirmed
        };
    }

    // Convert from Session model
    public static SessionDb FromSession(Session session)
    {
        return new SessionDb
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
            LastUpdated = DateTime.UtcNow
        };
    }
}
