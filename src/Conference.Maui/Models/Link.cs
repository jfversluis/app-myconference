using System.Text.Json.Serialization;

namespace Conference.Maui.Models;

public class Link
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("linkType")]
    public string LinkType { get; set; } = string.Empty;
}