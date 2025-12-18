namespace Conference.Maui.Models;

public class Session
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsServiceSession { get; set; }
    public bool IsPlenumSession { get; set; }
    public List<Speaker> Speakers { get; set; } = new();
    public List<string> CategoryItems { get; set; } = new();
    public string RoomId { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string LiveUrl { get; set; } = string.Empty;
    public string RecordingUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
