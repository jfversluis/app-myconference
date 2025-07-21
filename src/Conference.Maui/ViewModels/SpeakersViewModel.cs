using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class SpeakersViewModel : ObservableObject, IDisposable
{
    private readonly IEventDataService _eventService;

    public ObservableCollection<Speaker> Speakers { get; set; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    public SpeakersViewModel(IEventDataService eventDataService)
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
            // Reload data when it's refreshed in background
            var speakers = await _eventService.GetAllSpeakers();
            
            Speakers.Clear();
            foreach (var speaker in speakers)
            {
                Speakers.Add(speaker);
            }
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

    public async Task LoadSpeakersData()
    {
        if (IsLoading || IsRefreshing)
            return;

        try
        {
            IsLoading = true;

            var speakers = await _eventService.GetAllSpeakers();

            Speakers.Clear();
            foreach (var speaker in speakers)
            {
                Speakers.Add(speaker);
            }
        }
        finally
        {
            IsLoading = false;
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
            var speakers = await _eventService.GetAllSpeakers();

            Speakers.Clear();
            foreach (var speaker in speakers)
            {
                Speakers.Add(speaker);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage),
            new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
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
