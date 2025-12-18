using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Services;

namespace Conference.Maui.ViewModels;

public partial class SessionDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IFavoritesService _favoritesService;
    private readonly ISessionizeService _sessionizeService;

    [ObservableProperty]
    private Session? session;

    [ObservableProperty]
    private bool isFavorite;

    public SessionDetailViewModel(IFavoritesService favoritesService, ISessionizeService sessionizeService)
    {
        _favoritesService = favoritesService;
        _sessionizeService = sessionizeService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("Session"))
        {
            Session = query["Session"] as Session;
            Title = Session?.Title ?? "Session Details";
            _ = LoadFavoriteStatusAsync();
        }
    }

    private async Task LoadFavoriteStatusAsync()
    {
        if (Session != null)
        {
            IsFavorite = await _favoritesService.IsFavoriteAsync(Session.Id);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Session == null)
            return;

        if (IsFavorite)
        {
            await _favoritesService.RemoveFavoriteAsync(Session.Id);
        }
        else
        {
            await _favoritesService.AddFavoriteAsync(Session.Id);
        }

        IsFavorite = !IsFavorite;
    }

    [RelayCommand]
    private async Task SpeakerTappedAsync(Speaker speaker)
    {
        if (speaker == null)
            return;

        var allSpeakers = await _sessionizeService.GetSpeakersAsync();
        var fullSpeaker = allSpeakers.FirstOrDefault(s => s.Id == speaker.Id);

        if (fullSpeaker != null)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Speaker", fullSpeaker },
                { "Source", "SessionDetail" }
            };

            await Shell.Current.GoToAsync($"speakerdetail", navigationParameter);
        }
    }
}
