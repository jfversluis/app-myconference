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
        var speaker = new Speaker
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Bio = Bio,
            TagLine = TagLine,
            ProfilePicture = ProfilePicture,
            IsTopSpeaker = IsTopSpeaker,
            FullName = FullName
        };

        // Safely deserialize Links
        try
        {
            speaker.Links = string.IsNullOrEmpty(Links) ? [] : JsonSerializer.Deserialize<List<Link>>(Links) ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deserializing Links: {ex.Message}");
            speaker.Links = [];
        }

        // Safely deserialize SessionIds
        try
        {
            speaker.SessionIds = string.IsNullOrEmpty(SessionIds) ? [] : JsonSerializer.Deserialize<List<int>>(SessionIds) ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deserializing SessionIds: {ex.Message}");
            speaker.SessionIds = [];
        }

        return speaker;
    }

    // Create from Speaker model
    public static CachedSpeaker FromSpeaker(Speaker speaker)
    {
        var cachedSpeaker = new CachedSpeaker
        {
            Id = speaker.Id,
            FirstName = speaker.FirstName,
            LastName = speaker.LastName,
            Bio = speaker.Bio,
            TagLine = speaker.TagLine,
            ProfilePicture = speaker.ProfilePicture,
            IsTopSpeaker = speaker.IsTopSpeaker,
            FullName = speaker.FullName,
            CachedAt = DateTime.Now
        };

        // Safely serialize Links
        try
        {
            cachedSpeaker.Links = JsonSerializer.Serialize(speaker.Links);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error serializing Links: {ex.Message}");
            cachedSpeaker.Links = "[]";
        }

        // Safely serialize SessionIds
        try
        {
            cachedSpeaker.SessionIds = JsonSerializer.Serialize(speaker.SessionIds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error serializing SessionIds: {ex.Message}");
            cachedSpeaker.SessionIds = "[]";
        }

        return cachedSpeaker;
    }
}