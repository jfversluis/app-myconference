using SQLite;
using System.Text.Json;

namespace Conference.Maui.Models;

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
    
    public string SpeakerIdsJson { get; set; } = string.Empty;
    
    public string CategoryItemsJson { get; set; } = string.Empty;
    
    public string QuestionAnswersJson { get; set; } = string.Empty;
    
    public int RoomId { get; set; }
    
    public string Room { get; set; } = string.Empty;
    
    public string LiveUrl { get; set; } = string.Empty;
    
    public string RecordingUrl { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public bool IsInformed { get; set; }
    
    public bool IsConfirmed { get; set; }
    
    public DateTime CachedAt { get; set; }
    
    // Convert from Session to CachedSession
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
            SpeakerIdsJson = JsonSerializer.Serialize(session.SpeakerIds),
            CategoryItemsJson = JsonSerializer.Serialize(session.CategoryItems),
            QuestionAnswersJson = JsonSerializer.Serialize(session.QuestionAnswers),
            RoomId = session.RoomId,
            Room = session.Room,
            LiveUrl = session.LiveUrl,
            RecordingUrl = session.RecordingUrl,
            Status = session.Status,
            IsInformed = session.IsInformed,
            IsConfirmed = session.IsConfirmed,
            CachedAt = DateTime.UtcNow
        };
    }
    
    // Convert from CachedSession to Session
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
            SpeakerIds = string.IsNullOrEmpty(SpeakerIdsJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<string>>(SpeakerIdsJson) ?? [],
            CategoryItems = string.IsNullOrEmpty(CategoryItemsJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<object>>(CategoryItemsJson) ?? [],
            QuestionAnswers = string.IsNullOrEmpty(QuestionAnswersJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<object>>(QuestionAnswersJson) ?? [],
            RoomId = RoomId,
            Room = Room,
            LiveUrl = LiveUrl,
            RecordingUrl = RecordingUrl,
            Status = Status,
            IsInformed = IsInformed,
            IsConfirmed = IsConfirmed
        };
    }
}