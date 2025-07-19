using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class ScheduleViewModel(IEventDataService eventDataService) : ObservableObject
{
    private readonly IEventDataService _eventService = eventDataService;

    public ObservableCollection<Session> Sessions { get; set; } = [];
    public ObservableCollection<DaySchedule> ScheduleDays { get; set; } = [];
    public ObservableCollection<TimeSlot> TimeSlots { get; set; } = [];

    [ObservableProperty]
    private bool showTabs;

    public async Task LoadEventData()
    {
        var sessions = await _eventService.GetAllSessions();

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }

        // Group sessions by day
        GroupSessionsByDay();
    }

    private void GroupSessionsByDay()
    {
        ScheduleDays.Clear();
        TimeSlots.Clear();
        
        var groupedSessions = Sessions
            .GroupBy(s => s.StartsAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        ShowTabs = groupedSessions.Count > 1;

        foreach (var group in groupedSessions)
        {
            var daySchedule = new DaySchedule
            {
                Date = group.Key,
                TabTitle = $"{group.Key:dddd}, {group.Key:MMM dd}",
                Sessions = new ObservableCollection<Session>(group.OrderBy(s => s.StartsAt))
            };

            // Group sessions by start time within this day
            var timeGroups = group
                .GroupBy(s => s.StartsAt.TimeOfDay)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var timeGroup in timeGroups)
            {
                var timeSlot = new TimeSlot
                {
                    StartTime = group.Key.Add(timeGroup.Key),
                    TimeDisplayText = group.Key.Add(timeGroup.Key).ToString("HH:mm"),
                    Sessions = new ObservableCollection<Session>(timeGroup.OrderBy(s => s.RoomObject?.Sort ?? int.MaxValue).ThenBy(s => s.Title))
                };
                daySchedule.TimeSlots.Add(timeSlot);
            }

            ScheduleDays.Add(daySchedule);
        }

        // If there's only one day, also populate the direct TimeSlots collection
        if (ScheduleDays.Count == 1)
        {
            foreach (var timeSlot in ScheduleDays[0].TimeSlots)
            {
                TimeSlots.Add(timeSlot);
            }
        }
    }

    [RelayCommand]
    private async Task GoToSessionDetails(Session selectedSession)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailsPage),
            new Dictionary<string, object> { { "SelectedSession", selectedSession } });
    }
    [RelayCommand]
    private async Task GoToPickFavoriteSessionsPage()
    {
        await Shell.Current.GoToAsync(nameof(PickFavoriteSessionsPage), new Dictionary<string, object>()
        {
            { "AllSessions", Sessions.ToList()   }
        });
    }
}
