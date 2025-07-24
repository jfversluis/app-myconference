using SQLite;

namespace Conference.Maui.Models.Database;

[Table("Rooms")]
public class RoomDb
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Sort { get; set; }

    public DateTime LastUpdated { get; set; }

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

    // Convert from Room model
    public static RoomDb FromRoom(Room room)
    {
        return new RoomDb
        {
            Id = room.Id,
            Name = room.Name,
            Sort = room.Sort,
            LastUpdated = DateTime.UtcNow
        };
    }
}
