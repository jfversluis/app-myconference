namespace Conference.Maui.Models;

public class ConflictGroup
{
    public string TimeSlotLabel { get; }
    public List<SessionItem> Sessions { get; }
    public int SessionCount => Sessions.Count;

    public ConflictGroup(string timeSlotLabel, List<SessionItem> sessions)
    {
        TimeSlotLabel = timeSlotLabel;
        Sessions = sessions;
    }
}
