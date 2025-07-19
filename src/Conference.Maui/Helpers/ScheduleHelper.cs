using Conference.Maui.Models;
using System.Collections.ObjectModel;

namespace Conference.Maui.Helpers;

public static class ScheduleHelper
{
    /// <summary>
    /// Flattens a collection of TimeSlots into a mixed collection of TimeHeader and Session objects.
    /// This creates a flat list where each time slot is represented by a TimeHeader followed by its sessions.
    /// </summary>
    /// <param name="timeSlots">The time slots to flatten</param>
    /// <returns>A list containing TimeHeader and Session objects in sequence</returns>
    public static List<object> FlattenTimeSlots(IEnumerable<TimeSlot> timeSlots)
    {
        var flattenedItems = new List<object>();
        
        foreach (var timeSlot in timeSlots)
        {
            // Add time header
            flattenedItems.Add(new TimeHeader 
            { 
                TimeDisplayText = timeSlot.TimeDisplayText, 
                SessionCount = timeSlot.Sessions.Count 
            });
            
            // Add all sessions for this time slot
            foreach (var session in timeSlot.Sessions)
            {
                flattenedItems.Add(session);
            }
        }

        return flattenedItems;
    }
}
