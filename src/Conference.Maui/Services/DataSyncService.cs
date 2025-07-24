using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class DataSyncService
{
    private readonly IEventDataService _eventDataService;
    private readonly IDatabaseService _databaseService;
    private bool _isInitialized = false;

    public event EventHandler<bool>? DataRefreshed;
    public event EventHandler<string>? ErrorOccurred;

    public DataSyncService(IEventDataService eventDataService, IDatabaseService databaseService)
    {
        _eventDataService = eventDataService;
        _databaseService = databaseService;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Initialize database first
            await _databaseService.InitializeAsync();

            // Start background refresh
            _ = Task.Run(RefreshDataInBackground);

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to initialize data sync: {ex.Message}");
        }
    }

    public async Task<RefreshResult> RefreshDataAsync(bool forceRefresh = false)
    {
        try
        {
            if (forceRefresh)
            {
                // Force refresh by clearing hash
                await _databaseService.ClearEventDataAsync();
            }

            var result = await _eventDataService.RefreshDataAsync();
            DataRefreshed?.Invoke(this, result != RefreshResult.Failed);
            return result;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to refresh data: {ex.Message}");
            DataRefreshed?.Invoke(this, false);
            return RefreshResult.Failed;
        }
    }

    private async Task RefreshDataInBackground()
    {
        try
        {
            // Wait a bit before starting background refresh to not interfere with app startup
            await Task.Delay(2000);
            
            await _eventDataService.RefreshDataAsync();
            DataRefreshed?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            // Silent fail for background refresh
            System.Diagnostics.Debug.WriteLine($"Background data refresh failed: {ex.Message}");
            DataRefreshed?.Invoke(this, false);
        }
    }

    public async Task<bool> HasLocalDataAsync()
    {
        return await _databaseService.HasLocalDataAsync();
    }
}
