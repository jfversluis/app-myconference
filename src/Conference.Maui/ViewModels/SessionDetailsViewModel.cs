using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(SourceSpeakerId), "SourceSpeakerId")]
public partial class SessionDetailsViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<SessionDetailsViewModel> _logger;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    /// <summary>
    /// When navigating from a SpeakerDetailsPage, this holds the source speaker ID
    /// so we can pop back instead of pushing forward if the user taps that same speaker.
    /// </summary>
    [ObservableProperty]
    private string _sourceSpeakerId = string.Empty;

    [ObservableProperty]
    private SessionItem? _session;

    public SessionDetailsViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<SessionDetailsViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
        Title = "Session";

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session details for {SessionId}", SessionId);
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

        Session.IsFavorite = await _favoritesService.ToggleFavoriteAsync(Session.Id);
    }

    [RelayCommand]
    private async Task NavigateToSpeakerAsync(SpeakerItem speaker)
    {
        // If we came from this speaker, pop back instead of creating a loop
        if (!string.IsNullOrEmpty(SourceSpeakerId) && speaker.Id == SourceSpeakerId)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage), new Dictionary<string, object>
        {
            ["SpeakerId"] = speaker.Id,
            ["SourceSessionId"] = SessionId
        });
    }

    public void Receive(FavoriteChangedMessage message)
    {
        if (Session != null && Session.Id == message.SessionId)
        {
            Session.IsFavorite = message.IsFavorite;
        }
    }
}
