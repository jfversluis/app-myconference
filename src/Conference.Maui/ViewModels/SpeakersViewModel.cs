using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;

namespace Conference.Maui.ViewModels;

public partial class SpeakersViewModel : ObservableObject
{
    private readonly IEventDataService _eventService;
    private readonly DataSyncService _dataSyncService;
    private readonly IDatabaseService _databaseService;
    private readonly RefreshService _refreshService;
    private bool _isInitialized = false;

    public ObservableCollection<Speaker> Speakers { get; set; } = [];

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private bool hasData = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public SpeakersViewModel(IEventDataService eventDataService, DataSyncService dataSyncService, IDatabaseService databaseService, RefreshService refreshService)
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
        // If already initialized, just return instantly (no loading indicator)
        if (_isInitialized && HasData)
        {
            return;
        }

        // Check if we have cached data first for instant display
        var hasLocalData = await _databaseService.HasLocalDataAsync();
        
        if (hasLocalData && !_isInitialized)
        {
            // Load cached data immediately to show something to the user
            await LoadSpeakersData();
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
                        ErrorMessage = $"Background sync failed: {ex.Message}";
                    });
                }
            });
        }
        else if (!_isInitialized)
        {
            // No cached data, need to initialize first
            await _dataSyncService.InitializeAsync();
            await LoadSpeakersData();
            _isInitialized = true;
        }
    }

    public async Task LoadSpeakersData()
    {
        // Skip loading indicator if we already have data (for subsequent navigations)
        bool shouldShowLoading = !HasData;
        
        if (IsLoading)
            return;

        try
        {
            if (shouldShowLoading)
            {
                IsLoading = true;
            }
            ErrorMessage = string.Empty;

            var speakers = await _eventService.GetAllSpeakers();

            Speakers.Clear();
            foreach (var speaker in speakers.OrderBy(s => s.FirstName).ThenBy(s => s.LastName))
            {
                Speakers.Add(speaker);
            }

            HasData = Speakers.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load speakers: {ex.Message}";
            HasData = false;
        }
        finally
        {
            if (shouldShowLoading)
            {
                IsLoading = false;
            }
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
            
            var result = await _refreshService.RefreshWithFeedbackAsync("Speakers");
            
            if (result == RefreshResult.DataUpdated)
            {
                await LoadSpeakersData();
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
                await LoadSpeakersData();
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

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage),
            new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
    }
}
