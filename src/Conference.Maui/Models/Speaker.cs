namespace Conference.Maui.Models;

public class Speaker
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    public string Bio { get; set; } = string.Empty;

    public string TagLine { get; set; } = string.Empty;

    public string ProfilePictureUrl { get; set; } = string.Empty;

    public bool IsTopSpeaker { get; set; }

    public List<Session> Sessions { get; set; } = [];
}
