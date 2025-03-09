using System.Text.Json.Serialization;

namespace Conference.Maui.Models;

public class Room
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sort")]
    public int Sort { get; set; }
}