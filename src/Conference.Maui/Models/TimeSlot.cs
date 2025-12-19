using CommunityToolkit.Mvvm.ComponentModel;

namespace Conference.Maui.Models;

public class TimeSlot
{
    public string SlotStart { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public List<Session> Sessions { get; set; } = new();
}

public partial class DaySchedule : ObservableObject
{
    public DateTime Date { get; set; }
    public string DateString { get; set; } = string.Empty;
    public List<TimeSlot> TimeSlots { get; set; } = new();
    
    [ObservableProperty]
    private bool isSelected;
}
