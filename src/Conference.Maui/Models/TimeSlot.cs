namespace Conference.Maui.Models;

using System.Collections.ObjectModel;

public class TimeSlot : ObservableCollection<Session>
{
    public string SlotStart { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    
    public TimeSlot() : base()
    {
    }
    
    public TimeSlot(IEnumerable<Session> sessions) : base(sessions)
    {
    }
}

public class DaySchedule
{
    public DateTime Date { get; set; }
    public string DateString { get; set; } = string.Empty;
    public List<TimeSlot> TimeSlots { get; set; } = new();
}
