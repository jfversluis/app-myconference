namespace Conference.Maui.Models;

public class EventConfig
{
    public WifiConfig? Wifi { get; set; }
}

public class WifiConfig
{
    public string NetworkName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
