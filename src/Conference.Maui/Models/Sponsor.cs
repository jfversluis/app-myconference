namespace Conference.Maui.Models;

public class Sponsor
{
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public bool HasWebsite => !string.IsNullOrEmpty(Website);
}
