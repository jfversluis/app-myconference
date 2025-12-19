namespace Conference.Maui.Models;

public class Speaker
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string TagLine { get; set; } = string.Empty;
    public string ProfilePicture { get; set; } = string.Empty;
    public List<SessionLink> Sessions { get; set; } = new();
    public bool IsTopSpeaker { get; set; }
    public List<Link> Links { get; set; } = new();
}

public class SessionLink
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Link
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LinkType { get; set; } = string.Empty;
}
