using System.Text.Json.Serialization;

namespace Conference.Maui.Models;

public class Speaker
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;

    [JsonPropertyName("tagLine")]
    public string TagLine { get; set; } = string.Empty;

    [JsonPropertyName("profilePicture")]
    public string ProfilePicture { get; set; } = string.Empty;

    [JsonPropertyName("isTopSpeaker")]
    public bool IsTopSpeaker { get; set; }

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; } = [];

    [JsonPropertyName("sessions")]
    public List<int> SessionIds { get; set; } = [];

    [JsonIgnore]
    public List<Session> Sessions { get; set; } = [];

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("categoryItems")]
    public List<object> CategoryItems { get; set; } = [];

    [JsonPropertyName("questionAnswers")]
    public List<object> QuestionAnswers { get; set; } = [];
}
