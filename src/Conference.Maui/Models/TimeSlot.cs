using System.Collections.ObjectModel;

namespace Conference.Maui.Models;

public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public string TimeDisplayText { get; set; } = string.Empty;
    public ObservableCollection<Session> Sessions { get; set; } = [];
}
