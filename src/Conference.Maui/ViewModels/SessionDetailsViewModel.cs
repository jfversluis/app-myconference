using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SelectedSession), nameof(SelectedSession))]
public partial class SessionDetailsViewModel : ObservableObject
{
    private readonly IDatabaseService? _databaseService;

    [ObservableProperty]
    Session? _selectedSession;

    [ObservableProperty]
    private bool _isFavorite;

    public SessionDetailsViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    partial void OnSelectedSessionChanged(Session? value)
    {
        if (value != null && _databaseService != null)
        {
            _ = LoadFavoriteStatusAsync();
        }
    }

    private async Task LoadFavoriteStatusAsync()
    {
        if (SelectedSession != null && _databaseService != null)
        {
            IsFavorite = await _databaseService.IsFavoriteSessionAsync(SelectedSession.Id);
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite()
    {
        if (SelectedSession == null || _databaseService == null)
            return;

        if (IsFavorite)
        {
            // Remove from favorites
            await _databaseService.DeleteFavoriteSessionAsync(SelectedSession.Id);
            IsFavorite = false;
        }
        else
        {
            // Add to favorites
            var favoriteSession = new FavoriteSession
            {
                SessionId = SelectedSession.Id,
                IsFavorite = true,
                CreatedAt = DateTime.UtcNow
            };
            await _databaseService.SaveFavoriteSessionAsync(favoriteSession);
            IsFavorite = true;
        }
    }

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        await Shell.Current.GoToAsync("SpeakerDetails",
            new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
    }
}
