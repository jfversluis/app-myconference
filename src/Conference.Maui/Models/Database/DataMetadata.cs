using SQLite;

namespace Conference.Maui.Models.Database;

[Table("DataMetadata")]
public class DataMetadata
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }

    public DateTime CreatedAt { get; set; }
}
