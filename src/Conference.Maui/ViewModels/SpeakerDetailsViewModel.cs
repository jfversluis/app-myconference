using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SpeakerId), "SpeakerId")]
[QueryProperty(nameof(SourceSessionId), "SourceSessionId")]
public partial class SpeakerDetailsViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ISessionItemMapper _mapper;
    private readonly ILogger<SpeakerDetailsViewModel> _logger;

    [ObservableProperty]
    private string _speakerId = string.Empty;

    /// <summary>
    /// When navigating from a SessionDetailsPage, this holds the source session ID
    /// so we can pop back instead of pushing forward if the user taps that same session.
    /// </summary>
    [ObservableProperty]
    private string _sourceSessionId = string.Empty;

    [ObservableProperty]
    private SpeakerItem? _speaker;

    [ObservableProperty]
    private ObservableCollection<SessionItem> _sessions = [];

    public SpeakerDetailsViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ISessionItemMapper mapper,
        ILogger<SpeakerDetailsViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _mapper = mapper;
        _logger = logger;
        Title = "Speaker";

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    partial void OnSpeakerIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadSpeakerAsync();
        }
    }

    private async Task LoadSpeakerAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var allData = await _dataService.GetAllDataAsync();
            var speakerData = allData?.Speakers.FirstOrDefault(s => s.Id == SpeakerId);

            if (speakerData != null)
            {
                _mapper.Initialize(allData!);
                Speaker = SpeakerItem.FromSpeakerDetails(speakerData);
                Title = Speaker.FullName;

                var favorites = await _favoritesService.GetFavoriteSessionIdsAsync();
                var speakerSessions = allData!.Sessions
                    .Where(s => s.Speakers.Contains(speakerData.Id))
                    .OrderBy(s => s.StartsAt)
                    .Select(s => _mapper.MapSession(s, favoriteIds: favorites))
                    .ToList();

                Sessions = new ObservableCollection<SessionItem>(speakerSessions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading speaker details for {SpeakerId}", SpeakerId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToSessionAsync(SessionItem session)
    {
        // If we came from this session, pop back instead of creating a loop
        if (!string.IsNullOrEmpty(SourceSessionId) && session.Id == SourceSessionId)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
        {
            ["SessionId"] = session.Id,
            ["SourceSpeakerId"] = SpeakerId
        });
    }

    public void Receive(FavoriteChangedMessage message)
    {
        var session = Sessions.FirstOrDefault(s => s.Id == message.SessionId);
        if (session != null)
        {
            session.IsFavorite = message.IsFavorite;
        }
    }
}
