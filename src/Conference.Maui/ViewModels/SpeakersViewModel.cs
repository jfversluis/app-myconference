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
        Console.WriteLine("=== SpeakersViewModel: Constructor called ===");
        
        // Load data immediately when ViewModel is created
        _ = Task.Run(async () =>
        {
            await LoadDataAsync(false);
        });
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("=== SpeakersViewModel: InitializeAsync called ===");
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
            Console.WriteLine("=== SpeakersViewModel: LoadDataAsync started ===");

            _allSpeakers = await _sessionizeService.GetSpeakersAsync(forceRefresh);
            Console.WriteLine($"Loaded {_allSpeakers?.Count ?? 0} speakers from service");
            
            if (_allSpeakers == null || _allSpeakers.Count == 0)
            {
                Console.WriteLine("❌ No speakers returned from service!");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.Windows?.Count > 0)
                    {
                        var window = Application.Current.Windows[0];
                        if (window?.Page != null)
                        {
                            await window.Page.DisplayAlert("Debug Info", 
                                "No speakers were returned from the API. The service call succeeded but returned an empty list.", "OK");
                        }
                    }
                });
            }
            
            ApplyFilters();
            Console.WriteLine($"After ApplyFilters: Speakers.Count = {Speakers.Count}");
            
            if (Speakers.Count == 0 && _allSpeakers.Count > 0)
            {
                Console.WriteLine("❌ Speakers were filtered out!");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.Windows?.Count > 0)
                    {
                        var window = Application.Current.Windows[0];
                        if (window?.Page != null)
                        {
                            await window.Page.DisplayAlert("Debug Info", 
                                $"All {_allSpeakers.Count} speakers were filtered out after ApplyFilters.", "OK");
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading speakers: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
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
            Console.WriteLine("=== SpeakersViewModel: LoadDataAsync finished ===");
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
