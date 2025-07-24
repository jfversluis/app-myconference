using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Helpers;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;

namespace Conference.Maui.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly IEventDataService _eventService;
    private readonly DataSyncService _dataSyncService;
    private readonly IDatabaseService _databaseService;
    private readonly RefreshService _refreshService;
    private bool _isInitialized = false;

    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? DataChanged;

    public ObservableCollection<Session> Sessions { get; set; } = [];
    public ObservableCollection<DaySchedule> ScheduleDays { get; set; } = [];
    public ObservableCollection<object> FlattenedItems { get; set; } = [];

    [ObservableProperty]
    private bool showTabs;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private string loadingMessage = "Loading sessions...";

    [ObservableProperty]
    private bool hasData = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ScheduleViewModel(IEventDataService eventDataService, DataSyncService dataSyncService, IDatabaseService databaseService, RefreshService refreshService)
    {
        _eventService = eventDataService;
        _dataSyncService = dataSyncService;
        _databaseService = databaseService;
        _refreshService = refreshService;

        // Subscribe to data sync events
        _dataSyncService.DataRefreshed += OnDataRefreshed;
        _dataSyncService.ErrorOccurred += OnErrorOccurred;
    }

    public async Task InitializeAsync()
    {
        // If already initialized with data, return instantly without ANY async operations
        if (_isInitialized && HasData && Sessions.Count > 0)
        {
            return; // Ultra-fast path - no database calls, no await operations
        }

        // Only do database check if not yet initialized
        var hasLocalData = await _databaseService.HasLocalDataAsync();
        
        if (hasLocalData && !_isInitialized)
        {
            // Load cached data immediately to show something to the user
            await LoadEventData();
            _isInitialized = true;
            
            // Initialize data sync in background for future updates
            _ = Task.Run(async () =>
            {
                try
                {
                    await _dataSyncService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ErrorOccurred?.Invoke(this, $"Background sync failed: {ex.Message}");
                    });
                }
            });
        }
        else if (!_isInitialized)
        {
            // No cached data, need to initialize first
            await _dataSyncService.InitializeAsync();
            await LoadEventData();
            _isInitialized = true;
        }
    }

    public async Task LoadEventData()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Check if we have local data
            var hasLocalData = await _dataSyncService.HasLocalDataAsync();
            
            if (!hasLocalData)
            {
                LoadingMessage = "Loading event data...";
            }
            else
            {
                LoadingMessage = "Loading sessions...";
            }

            var sessions = await _eventService.GetAllSessions();

            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }

            // Group sessions by day
            await GroupSessionsByDay();
            HasData = Sessions.Count > 0;

            if (!hasLocalData && HasData)
            {
                // Data was loaded from remote, trigger background refresh to check for updates
                // But not during an explicit refresh operation
                if (!IsRefreshing)
                {
                    _ = Task.Run(async () => { var _ = await _dataSyncService.RefreshDataAsync(); });
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load session data: {ex.Message}";
            HasData = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing)
            return;

        try
        {
            IsRefreshing = true;
            ErrorMessage = string.Empty;
            
            var result = await _refreshService.RefreshWithFeedbackAsync("Schedule");
            
            if (result == RefreshResult.DataUpdated)
            {
                await LoadEventData();
            }
        }
        catch (Exception ex)
        {
            var errorToast = Toast.Make($"Failed to refresh: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await errorToast.Show();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async void OnDataRefreshed(object? sender, bool success)
    {
        if (success)
        {
            // Reload data on UI thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await LoadEventData();
            });
        }
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorMessage = error;
        });
    }

    private async Task GroupSessionsByDay()
    {
        ScheduleDays.Clear();
        FlattenedItems.Clear();

        if (!Sessions.Any())
        {
            ShowTabs = false;
            return;
        }

        // Perform heavy computations off the UI thread
        var computedData = await Task.Run(() =>
        {
            var daySchedules = new List<DaySchedule>();
            
            var groupedSessions = Sessions
                .GroupBy(s => s.StartsAt.Date)
                .OrderBy(g => g.Key)
                .ToList();

            // Use short day notation if more than 2 days
            bool useShortDayNotation = groupedSessions.Count > 2;

            foreach (var group in groupedSessions)
            {
                var daySchedule = new DaySchedule
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
                    var timeSlot = new TimeSlot
                    {
                        StartTime = group.Key.Add(timeGroup.Key),
                        TimeDisplayText = group.Key.Add(timeGroup.Key).ToString("HH:mm"),
                        Sessions = new ObservableCollection<Session>(timeGroup.OrderBy(s => s.RoomObject?.Sort ?? 99).ThenBy(s => s.Title))
                    };
                    daySchedule.TimeSlots.Add(timeSlot);
                }

                // Pre-compute flattened items for this day to avoid UI processing
                daySchedule.FlattenedItems = ScheduleHelper.FlattenTimeSlots(daySchedule.TimeSlots);

                daySchedules.Add(daySchedule);
            }

            // If there's only one day, compute flattened items
            List<object>? flattenedItems = null;
            if (daySchedules.Count == 1)
            {
                flattenedItems = ScheduleHelper.FlattenTimeSlots(daySchedules[0].TimeSlots);
            }

            return new { DaySchedules = daySchedules, FlattenedItems = flattenedItems, ShowTabs = groupedSessions.Count > 1 };
        });

        // Update UI collections on main thread
        foreach (var daySchedule in computedData.DaySchedules)
        {
            ScheduleDays.Add(daySchedule);
        }

        if (computedData.FlattenedItems != null)
        {
            foreach (var item in computedData.FlattenedItems)
            {
                FlattenedItems.Add(item);
            }
        }

        ShowTabs = computedData.ShowTabs;
        
        // Notify that data has changed
        DataChanged?.Invoke(this, EventArgs.Empty);
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
