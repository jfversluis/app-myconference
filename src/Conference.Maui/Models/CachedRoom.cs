using SQLite;

namespace Conference.Maui.Models;

public class CachedRoom
{
    [PrimaryKey]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Sort { get; set; }
    
    public DateTime CachedAt { get; set; }
    
    // Convert from Room to CachedRoom
    public static CachedRoom FromRoom(Room room)
    {
        return new CachedRoom
        {
            Id = room.Id,
            Name = room.Name,
            Sort = room.Sort,
            CachedAt = DateTime.UtcNow
        };
    }
    
    // Convert from CachedRoom to Room
    public Room ToRoom()
    {
        return new Room
        {
            Id = Id,
            Name = Name,
            Sort = Sort
        };
    }
}