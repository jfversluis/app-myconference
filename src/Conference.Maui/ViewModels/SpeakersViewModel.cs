using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;

namespace Conference.Maui.ViewModels;

public partial class SpeakersViewModel : BaseViewModel
{
    private readonly IConferenceDataService _dataService;
    private readonly ILogger<SpeakersViewModel> _logger;
    private List<SpeakerItem> _allSpeakers = [];

    [ObservableProperty]
    private ObservableCollection<SpeakerItem> _speakers = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    public bool ShowEmptyState => !IsBusy && Speakers.Count == 0;

    public SpeakersViewModel(
        IConferenceDataService dataService,
        ILogger<SpeakersViewModel> logger)
    {
        _dataService = dataService;
        _logger = logger;
        Title = "Speakers";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var allData = await _dataService.GetAllDataAsync();

            if (allData == null)
            {
                _logger.LogWarning("No data received from service");
                return;
            }

            _allSpeakers = allData.Speakers
                .Select(SpeakerItem.FromSpeakerDetails)
                .OrderBy(s => s.FullName)
                .ToList();

            ApplySearch();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading speakers");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearch();
    }

    private void ApplySearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Speakers = new ObservableCollection<SpeakerItem>(_allSpeakers);
            OnPropertyChanged(nameof(ShowEmptyState));
            return;
        }

        var filtered = _allSpeakers
            .Where(s =>
                s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (s.TagLine?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        Speakers = new ObservableCollection<SpeakerItem>(filtered);
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private async Task NavigateToSpeakerDetailsAsync(SpeakerItem speaker)
    {
        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage), new Dictionary<string, object>
        {
            ["SpeakerId"] = speaker.Id
        });
    }
}
