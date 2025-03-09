using System.Text.Json.Serialization;

namespace Conference.Maui.Models;

public class AllData
{
    [JsonPropertyName("sessions")]
    public List<Session>? Sessions { get; set; }

    [JsonPropertyName("speakers")]
    public List<Speaker>? Speakers { get; set; }

    [JsonPropertyName("questions")]
    public List<object>? Questions { get; set; }

    [JsonPropertyName("categories")]
    public List<object>? Categories { get; set; }

    [JsonPropertyName("rooms")]
    public List<Room>? Rooms { get; set; }
}