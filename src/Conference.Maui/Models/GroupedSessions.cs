using System.Collections.ObjectModel;

namespace Conference.Maui.Models;

public class GroupedSessions : ObservableCollection<Session>
{
    public string Key => SlotStart;
    public string SlotStart { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    
    public GroupedSessions(TimeSlot timeSlot) : base(timeSlot.Sessions)
    {
        SlotStart = timeSlot.SlotStart;
        StartsAt = timeSlot.StartsAt;
        EndsAt = timeSlot.EndsAt;
    }
}
