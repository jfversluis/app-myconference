using SQLite;

namespace Conference.Maui.Models;

[Table("CachedRooms")]
public class CachedRoom
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Sort { get; set; }

    public DateTime CachedAt { get; set; }

    // Convert to Room model
    public Room ToRoom()
    {
        return new Room
        {
            Id = Id,
            Name = Name,
            Sort = Sort
        };
    }

    // Create from Room model
    public static CachedRoom FromRoom(Room room)
    {
        return new CachedRoom
        {
            Id = room.Id,
            Name = room.Name,
            Sort = room.Sort,
            CachedAt = DateTime.Now
        };
    }
}