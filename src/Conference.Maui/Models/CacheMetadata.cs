using SQLite;

namespace Conference.Maui.Models;

public class CacheMetadata
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    
    public DateTime LastUpdated { get; set; }
    
    public string Version { get; set; } = string.Empty;
}