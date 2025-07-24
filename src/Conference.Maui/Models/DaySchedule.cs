using System.Collections.ObjectModel;

namespace Conference.Maui.Models;

public class DaySchedule
{
    public DateTime Date { get; set; }
    public string TabTitle { get; set; } = string.Empty;
    public ObservableCollection<Session> Sessions { get; set; } = [];
    public ObservableCollection<TimeSlot> TimeSlots { get; set; } = [];
    public List<object> FlattenedItems { get; set; } = [];
}
