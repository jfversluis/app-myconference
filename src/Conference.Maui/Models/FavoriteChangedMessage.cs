namespace Conference.Maui.Models;

public class FavoriteChangedMessage(string sessionId, bool isFavorite)
{
    public string SessionId { get; } = sessionId;
    public bool IsFavorite { get; } = isFavorite;
}
