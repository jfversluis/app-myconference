using SQLite;
using System.Text.Json;

namespace Conference.Maui.Models;

[Table("CachedSpeakers")]
public class CachedSpeaker
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string TagLine { get; set; } = string.Empty;

    public string ProfilePicture { get; set; } = string.Empty;

    public bool IsTopSpeaker { get; set; }

    public string Links { get; set; } = string.Empty; // JSON serialized list

    public string SessionIds { get; set; } = string.Empty; // JSON serialized list

    public string FullName { get; set; } = string.Empty;

    public DateTime CachedAt { get; set; }

    // Convert to Speaker model
    public Speaker ToSpeaker()
    {
        return new Speaker
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Bio = Bio,
            TagLine = TagLine,
            ProfilePicture = ProfilePicture,
            IsTopSpeaker = IsTopSpeaker,
            Links = string.IsNullOrEmpty(Links) ? [] : JsonSerializer.Deserialize<List<Link>>(Links) ?? [],
            SessionIds = string.IsNullOrEmpty(SessionIds) ? [] : JsonSerializer.Deserialize<List<int>>(SessionIds) ?? [],
            FullName = FullName
        };
    }

    // Create from Speaker model
    public static CachedSpeaker FromSpeaker(Speaker speaker)
    {
        return new CachedSpeaker
        {
            Id = speaker.Id,
            FirstName = speaker.FirstName,
            LastName = speaker.LastName,
            Bio = speaker.Bio,
            TagLine = speaker.TagLine,
            ProfilePicture = speaker.ProfilePicture,
            IsTopSpeaker = speaker.IsTopSpeaker,
            Links = JsonSerializer.Serialize(speaker.Links),
            SessionIds = JsonSerializer.Serialize(speaker.SessionIds),
            FullName = speaker.FullName,
            CachedAt = DateTime.Now
        };
    }
}