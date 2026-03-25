namespace Conference.Maui.Models;

/// <summary>
/// Persisted state for the Quick Pick session discovery feature.
/// Stored in Akavache so users can resume where they left off.
/// </summary>
public class SwipeState
{
    public List<string> SkippedSessionIds { get; set; } = [];
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
