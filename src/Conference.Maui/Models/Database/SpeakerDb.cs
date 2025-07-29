using SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conference.Maui.Models.Database;

[Table("Speakers")]
public class SpeakerDb
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

    public DateTime LastUpdated { get; set; }

    // Convert to Speaker model
    public Speaker ToSpeaker()
    {
        var linksList = string.IsNullOrEmpty(Links) ? 
            new List<Link>() : 
            JsonSerializer.Deserialize<List<Link>>(Links) ?? new List<Link>();

        var sessionIdList = string.IsNullOrEmpty(SessionIds) ? 
            new List<int>() : 
            JsonSerializer.Deserialize<List<int>>(SessionIds) ?? new List<int>();

        return new Speaker
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Bio = Bio,
            TagLine = TagLine,
            ProfilePicture = ProfilePicture,
            IsTopSpeaker = IsTopSpeaker,
            Links = linksList,
            SessionIds = sessionIdList,
            FullName = FullName,
            Sessions = new List<Session>() // Will be populated later
        };
    }

    // Convert from Speaker model
    public static SpeakerDb FromSpeaker(Speaker speaker)
    {
        return new SpeakerDb
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
            LastUpdated = DateTime.UtcNow
        };
    }
}
