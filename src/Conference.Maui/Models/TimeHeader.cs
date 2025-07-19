namespace Conference.Maui.Models;

public class TimeHeader
{
    private string _timeDisplayText = string.Empty;
    
    public string TimeDisplayText 
    { 
        get => _timeDisplayText;
        set => _timeDisplayText = value ?? string.Empty;
    }
    
    public int SessionCount { get; set; }
}
