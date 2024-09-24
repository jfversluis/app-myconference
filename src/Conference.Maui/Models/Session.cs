namespace Conference.Maui.Models;

public class Session
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool IsServiceSession { get; set; }

    public bool IsPlenumSession { get; set; }

    public int RoomId { get; set; }

    public string Room { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsInformed { get; set; }

    public bool IsConfirmed { get; set; }

    public List<Speaker> Speakers { get; set; } = [];
}
