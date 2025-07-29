using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Services;

namespace Conference.Maui.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private readonly DataSyncService _dataSyncService;
    private readonly RefreshService _refreshService;

    [ObservableProperty]
    private string lastDataSync = "Never";

    [ObservableProperty]
    private bool isSyncing = false;

    [ObservableProperty]
    private string syncStatus = "Ready";

    public AboutViewModel(DataSyncService dataSyncService, RefreshService refreshService)
    {
        _dataSyncService = dataSyncService;
        _refreshService = refreshService;
        
        // Subscribe to events
        _dataSyncService.DataRefreshed += OnDataRefreshed;
        _dataSyncService.ErrorOccurred += OnErrorOccurred;
        
        // Initialize with current time
        UpdateLastSyncTime();
    }

    [RelayCommand]
    private async Task ForceRefresh()
    {
        if (IsSyncing)
            return;

        IsSyncing = true;
        SyncStatus = "Syncing...";

        try
        {
            var result = await _refreshService.RefreshWithFeedbackAsync("Data", forceRefresh: true);
            
            if (result == Conference.Maui.Models.RefreshResult.DataUpdated)
            {
                SyncStatus = "Sync completed";
            }
            else if (result == Conference.Maui.Models.RefreshResult.NoUpdateNeeded)
            {
                SyncStatus = "Already up to date";
            }
            else
            {
                SyncStatus = "Sync failed";
            }
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void OnDataRefreshed(object? sender, bool success)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsSyncing = false;
            if (success)
            {
                SyncStatus = "Sync completed";
                UpdateLastSyncTime();
            }
            else
            {
                SyncStatus = "Sync failed";
            }
        });
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsSyncing = false;
            SyncStatus = $"Error: {error}";
        });
    }

    private void UpdateLastSyncTime()
    {
        LastDataSync = DateTime.Now.ToString("MMM dd, HH:mm");
    }
}
