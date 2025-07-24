using SQLite;

namespace Conference.Maui.Models;

public class FavoriteSession
{
    [PrimaryKey]
    public string SessionId { get; set; } = string.Empty;
    
    public bool IsFavorite { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
