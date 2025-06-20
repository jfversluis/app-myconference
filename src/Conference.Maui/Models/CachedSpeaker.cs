using SQLite;
using System.Text.Json;

namespace Conference.Maui.Models;

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
    
    public string LinksJson { get; set; } = string.Empty;
    
    public string SessionIdsJson { get; set; } = string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    
    public string CategoryItemsJson { get; set; } = string.Empty;
    
    public string QuestionAnswersJson { get; set; } = string.Empty;
    
    public DateTime CachedAt { get; set; }
    
    // Convert from Speaker to CachedSpeaker
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
            LinksJson = JsonSerializer.Serialize(speaker.Links),
            SessionIdsJson = JsonSerializer.Serialize(speaker.SessionIds),
            FullName = speaker.FullName,
            CategoryItemsJson = JsonSerializer.Serialize(speaker.CategoryItems),
            QuestionAnswersJson = JsonSerializer.Serialize(speaker.QuestionAnswers),
            CachedAt = DateTime.UtcNow
        };
    }
    
    // Convert from CachedSpeaker to Speaker
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
            Links = string.IsNullOrEmpty(LinksJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<Link>>(LinksJson) ?? [],
            SessionIds = string.IsNullOrEmpty(SessionIdsJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<int>>(SessionIdsJson) ?? [],
            FullName = FullName,
            CategoryItems = string.IsNullOrEmpty(CategoryItemsJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<object>>(CategoryItemsJson) ?? [],
            QuestionAnswers = string.IsNullOrEmpty(QuestionAnswersJson) 
                ? [] 
                : JsonSerializer.Deserialize<List<object>>(QuestionAnswersJson) ?? []
        };
    }
}