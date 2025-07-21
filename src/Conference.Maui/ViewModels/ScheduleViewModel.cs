using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Helpers;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class ScheduleViewModel : ObservableObject, IDisposable
{
    private readonly IEventDataService _eventService;

    public ObservableCollection<Session> Sessions { get; set; } = [];
    public ObservableCollection<DaySchedule> ScheduleDays { get; set; } = [];
    public ObservableCollection<object> FlattenedItems { get; set; } = [];

    [ObservableProperty]
    private bool showTabs;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string lastRefreshText = string.Empty;

    public ScheduleViewModel(IEventDataService eventDataService)
    {
        _eventService = eventDataService;
        
        // Subscribe to data refresh events
        _eventService.DataRefreshed += OnDataRefreshed;
        _eventService.RefreshStateChanged += OnRefreshStateChanged;
    }

    private async void OnDataRefreshed(object? sender, EventArgs e)
    {
        // Ensure UI updates happen on the main thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Update last refresh text
            await UpdateLastRefreshText();
            
            // Reload data when it's refreshed in background
            var sessions = await _eventService.GetAllSessions();
            
            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }
            
            GroupSessionsByDay();
        });
    }

    private void OnRefreshStateChanged(object? sender, bool isRefreshing)
    {
        // Ensure UI updates happen on the main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update UI when background refresh state changes
            if (!IsLoading) // Don't override manual refresh
            {
                IsRefreshing = isRefreshing;
            }
        });
    }

    public async Task LoadEventData()
    {
        if (IsLoading || IsRefreshing)
            return;

        try
        {
            IsLoading = true;

            var sessions = await _eventService.GetAllSessions();

            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }

            // Group sessions by day
            GroupSessionsByDay();
            
            // Update last refresh text
            await UpdateLastRefreshText();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateLastRefreshText()
    {
        var lastRefresh = await _eventService.GetLastRefreshTimeAsync();
        if (lastRefresh.HasValue)
        {
            LastRefreshText = $"Last updated: {lastRefresh.Value:MMM dd, HH:mm}";
        }
        else
        {
            LastRefreshText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RefreshData()
    {
        if (IsLoading || IsRefreshing)
            return;

        try
        {
            IsRefreshing = true;

            // Force refresh from remote
            await _eventService.RefreshDataAsync(forceRefresh: true);

            // Reload the data
            var sessions = await _eventService.GetAllSessions();

            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }

            // Group sessions by day
            GroupSessionsByDay();
            
            // Update last refresh text
            await UpdateLastRefreshText();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void GroupSessionsByDay()
    {
        ScheduleDays.Clear();
        FlattenedItems.Clear();
        
        var groupedSessions = Sessions
            .GroupBy(s => s.StartsAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        ShowTabs = groupedSessions.Count > 1;

        // Use short day notation if more than 2 days
        bool useShortDayNotation = groupedSessions.Count > 2;

        foreach (var group in groupedSessions)
        {
            DaySchedule daySchedule = new()
            {
                Date = group.Key,
                TabTitle = useShortDayNotation ? 
                    $"{group.Key:ddd}, {group.Key:MMM dd}" :  // Short format: "Mon, Sep 10"
                    $"{group.Key:dddd}, {group.Key:MMM dd}",  // Long format: "Monday, Sep 10"
                Sessions = new ObservableCollection<Session>(group.OrderBy(s => s.RoomObject?.Sort ?? 99).ThenBy(s => s.Title))
            };

            // Group sessions by start time within this day
            var timeGroups = group
                .GroupBy(s => s.StartsAt.TimeOfDay)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var timeGroup in timeGroups)
            {
                TimeSlot timeSlot = new()
                {
                    StartTime = group.Key.Add(timeGroup.Key),
                    TimeDisplayText = group.Key.Add(timeGroup.Key).ToString("HH:mm"),
                    Sessions = new ObservableCollection<Session>(timeGroup.OrderBy(s => s.RoomObject?.Sort ?? 99).ThenBy(s => s.Title))
                };
                daySchedule.TimeSlots.Add(timeSlot);
            }

            ScheduleDays.Add(daySchedule);
        }

        // If there's only one day, populate the flattened items collection
        if (ScheduleDays.Count == 1)
        {
            var flattenedItems = ScheduleHelper.FlattenTimeSlots(ScheduleDays[0].TimeSlots);
            foreach (var item in flattenedItems)
            {
                FlattenedItems.Add(item);
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

    public void Dispose()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_eventService != null)
        {
            _eventService.DataRefreshed -= OnDataRefreshed;
            _eventService.RefreshStateChanged -= OnRefreshStateChanged;
        }
    }
}
