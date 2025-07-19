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
            ScheduleDays.Add(daySchedule);
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
