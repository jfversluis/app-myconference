using SQLite;

namespace Conference.Maui.Models;

[Table("DataCache")]
public class DataCacheInfo
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }

    public DateTime LastChecked { get; set; }

    public bool IsRefreshing { get; set; } = false;
}