using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class SessionDetailsViewModel : BaseViewModel
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private SessionItem? _session;

    public SessionDetailsViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        Title = "Session";
    }

    partial void OnSessionIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadSessionAsync();
        }
    }

    private async Task LoadSessionAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var allData = await _dataService.GetAllDataAsync();
            var sessionData = allData?.Sessions.FirstOrDefault(s => s.Id == SessionId);
            
            if (sessionData != null)
            {
                var favorites = await _favoritesService.GetFavoriteSessionIdsAsync();
                var speakers = sessionData.Speakers
                    .Select(speakerId => allData?.Speakers.FirstOrDefault(s => s.Id == speakerId))
                    .Where(s => s != null)
                    .Select(s => SpeakerItem.FromSpeakerDetails(s!))
                    .ToList();

                Session = new SessionItem
                {
                    Id = sessionData.Id,
                    Title = sessionData.Title,
                    Description = sessionData.Description,
                    StartsAt = sessionData.StartsAt,
                    EndsAt = sessionData.EndsAt,
                    RoomName = allData?.Rooms.FirstOrDefault(r => r.Id == sessionData.RoomId)?.Name,
                    RoomId = sessionData.RoomId,
                    Speakers = speakers,
                    IsFavorite = favorites.Contains(sessionData.Id)
                };

                Title = Session.Title;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Session == null) return;

        if (Session.IsFavorite)
        {
            await _favoritesService.RemoveFavoriteAsync(Session.Id);
        }
        else
        {
            await _favoritesService.AddFavoriteAsync(Session.Id);
        }
        Session.IsFavorite = !Session.IsFavorite;
    }

    [RelayCommand]
    private async Task NavigateToSpeakerAsync(SpeakerItem speaker)
    {
        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage), new Dictionary<string, object>
        {
            ["SpeakerId"] = speaker.Id
        });
    }
}
