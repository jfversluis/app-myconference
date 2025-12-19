using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Services;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class SpeakersViewModel : BaseViewModel
{
    private readonly ISessionizeService _sessionizeService;
    private List<Speaker> _allSpeakers = new();

    [ObservableProperty]
    private ObservableCollection<Speaker> speakers = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    public SpeakersViewModel(ISessionizeService sessionizeService)
    {
        _sessionizeService = sessionizeService;
        Title = "Speakers";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync(false);
    }

    [RelayCommand]
    private async Task LoadDataAsync(bool forceRefresh)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            System.Diagnostics.Debug.WriteLine("=== SpeakersViewModel: LoadDataAsync started ===");

            _allSpeakers = await _sessionizeService.GetSpeakersAsync(forceRefresh);
            System.Diagnostics.Debug.WriteLine($"Loaded {_allSpeakers?.Count ?? 0} speakers from service");
            
            ApplyFilters();
            System.Diagnostics.Debug.WriteLine($"After ApplyFilters: Speakers.Count = {Speakers.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading speakers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Application.Current?.Windows?.Count > 0)
                {
                    var window = Application.Current.Windows[0];
                    if (window?.Page != null)
                    {
                        await window.Page.DisplayAlert("Error", 
                            $"Failed to load speakers: {ex.Message}", "OK");
                    }
                }
            });
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            System.Diagnostics.Debug.WriteLine("=== SpeakersViewModel: LoadDataAsync finished ===");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync(true);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        Speakers.Clear();

        var filtered = _allSpeakers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(s =>
                s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.TagLine.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Bio.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var speaker in filtered.OrderBy(s => s.FullName))
        {
            Speakers.Add(speaker);
        }
    }

    [RelayCommand]
    private async Task SpeakerTappedAsync(Speaker speaker)
    {
        if (speaker == null)
            return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Speaker", speaker }
        };

        await Shell.Current.GoToAsync($"speakerdetail", navigationParameter);
    }
}
