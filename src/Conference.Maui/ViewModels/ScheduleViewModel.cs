using CommunityToolkit.Mvvm.ComponentModel;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class ScheduleViewModel(IEventDataService eventDataService) : ObservableObject
{
    private readonly IEventDataService _eventService = eventDataService;

    public ObservableCollection<Session> Sessions { get; set; } = [];

    public async Task LoadEventData()
    {
        var sessions = await _eventService.GetAllSessions();

        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }
    }
}
